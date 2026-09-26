using System.Net;
using System.Net.Http;
using System.Text.Json;
using Elsa.Studio.UserTasks.Models;
using Elsa.Studio.UserTasks.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.UserTasks.Tests;

/// <summary>
/// Pins the client's half of the wire contract. Each payload here is the shape the Core endpoints emit, so a
/// drift on either side shows up as a deserialization gap rather than as blank fields in the UI.
/// </summary>
public class UserTaskContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions)!;

    [Fact]
    public void Detail_DeserializesTheFlatServerShapeIncludingInheritedSummaryFields()
    {
        const string payload = """
        {
          "id": "task-1",
          "title": "Approve invoice",
          "summary": "Invoice 42 needs approval",
          "taskType": "approval",
          "status": "Assigned",
          "priority": 70,
          "assignee": { "kind": "user", "provider": "oidc", "id": "u1", "displayName": "Alice" },
          "candidateSummary": "2 users",
          "dueAt": "2026-01-01T12:00:00+00:00",
          "isOverdue": true,
          "createdAt": "2025-12-01T09:00:00+00:00",
          "updatedAt": "2025-12-02T09:00:00+00:00",
          "workflowDefinitionName": "Invoice approval",
          "workflowDefinitionVersion": 3,
          "workflowInstanceReference": "correlation-1",
          "allowedActions": ["complete", "release"],
          "revision": 4,
          "instructions": "Check the totals",
          "disclosure": { "canViewProtected": true, "canViewWorkflow": true, "canViewHistory": true, "guestVisible": false },
          "workflow": { "definitionId": "def-1", "definitionName": "Invoice approval", "definitionVersion": 3, "instanceId": "inst-1", "instanceReference": "correlation-1" },
          "form": {
            "provider": "test",
            "key": "invoice",
            "version": "v1",
            "fields": [
              { "key": "note", "label": "Note", "type": "text", "required": true, "masked": false, "canReveal": false, "value": "hello" },
              { "key": "iban", "label": "IBAN", "type": "text", "required": false, "masked": true, "canReveal": true }
            ],
            "actions": [{ "key": "Approve", "label": "Approve" }]
          },
          "actions": [{ "key": "Approve", "label": "Approve" }],
          "outcome": null,
          "completedBy": null
        }
        """;

        var detail = Deserialize<UserTaskDetail>(payload);

        // Inherited summary fields must survive: the QA finding was that a nested shape left them default.
        Assert.Equal("task-1", detail.Id);
        Assert.Equal("Approve invoice", detail.Title);
        Assert.Equal(70, detail.Priority);
        Assert.Equal(4, detail.Revision);
        Assert.True(detail.IsOverdue);
        Assert.Equal("Alice", detail.Assignee?.Label);
        Assert.Equal(3, detail.WorkflowDefinitionVersion);
        Assert.True(detail.Allows(UserTaskActions.Complete));
        Assert.True(detail.Disclosure.CanViewProtected);
        Assert.Equal(2, detail.Form?.Fields.Count);

        // A masked field arrives without a value; it is disclosed only through the reveal command.
        var masked = detail.Form!.Fields.Single(x => x.Masked);
        Assert.True(masked.CanReveal);
        Assert.Null(masked.Value);
    }

    [Theory]
    [InlineData("Completed", true, false)]
    [InlineData("Cancelled", true, false)]
    [InlineData("TimedOut", true, false)]
    [InlineData("Completing", false, true)]
    [InlineData("Cancelling", false, true)]
    [InlineData("TimingOut", false, true)]
    [InlineData("Assigned", false, false)]
    public void Summary_ClassifiesTerminalAndTransitionalStates(string status, bool terminal, bool transitioning)
    {
        var summary = new UserTaskSummary { Status = status };

        Assert.Equal(terminal, summary.IsTerminal);
        Assert.Equal(transitioning, summary.IsTransitioning);
    }

    [Fact]
    public void OperationResponse_DeserializesTheCommandEnvelope()
    {
        var response = Deserialize<UserTaskOperationResponse>(
            """{ "operationId": "op-1", "status": "accepted", "revision": 5, "task": { "id": "task-1", "status": "Completing", "revision": 5 } }""");

        Assert.Equal("op-1", response.OperationId);
        Assert.Equal("accepted", response.Status);
        Assert.Equal(5, response.Revision);
        Assert.Equal("Completing", response.Task?.Status);
    }

    [Fact]
    public void FeatureCapabilities_DeserializeTheDescriptorTheMenuGatesOn()
    {
        var capabilities = Deserialize<UserTaskFeatureCapabilities>(
            """{ "enabled": true, "canList": true, "canRead": true, "canReadAll": false, "canClaim": true, "canComplete": true, "canRelease": true, "canAssign": false, "canUpdate": false, "canCancel": false, "canCreateGuestLinks": false, "canViewProtected": true, "participantPicker": false, "realtime": true, "pollingIntervalSeconds": 30 }""");

        Assert.True(capabilities is { Enabled: true, CanList: true, CanRead: true, Realtime: true });
        Assert.False(capabilities.CanReadAll);
        Assert.Equal(30, capabilities.PollingIntervalSeconds);
    }

    [Fact]
    public void GuestChallenge_DefaultsToRequiringACodeSoTheSurfaceFailsClosed()
    {
        var challenge = Deserialize<UserTaskGuestChallenge>("""{ "challengeType": "code", "prompt": "Enter the verification code you were sent to continue." }""");

        Assert.Equal("code", challenge.ChallengeType);
        Assert.True(challenge.RequiresCode);
    }

    [Fact]
    public void Events_ProjectSafeAuditFieldsOnly()
    {
        var events = Deserialize<UserTaskEventsResponse>(
            """{ "items": [{ "id": "e1", "kind": "Claimed", "summary": null, "occurredAt": "2026-01-01T12:00:00+00:00", "actorDisplayName": "Alice" }], "nextCursor": "abc" }""");

        var item = Assert.Single(events.Items);
        Assert.Equal("Claimed", item.Kind);
        Assert.Equal("Alice", item.ActorDisplayName);
        Assert.Equal("abc", events.NextCursor);
    }
}

/// <summary>
/// The error mapper is what stands between a transport exception and the screen. It must always produce
/// display-safe copy and must classify conflicts so the caller knows to reload rather than retry blindly.
/// </summary>
public class UserTaskErrorMapperTests
{
    private static ApiException CreateApiException(HttpStatusCode statusCode, string? content) =>
        ApiException.Create(
            new HttpRequestMessage(HttpMethod.Post, "https://elsa.example/user-tasks/task-1/complete"),
            HttpMethod.Post,
            new HttpResponseMessage(statusCode) { Content = content == null ? null : new StringContent(content) },
            new RefitSettings()).GetAwaiter().GetResult();

    [Fact]
    public void Describe_UsesTheServersSafeCodeAndMessage()
    {
        var error = UserTaskErrorMapper.Describe(CreateApiException(HttpStatusCode.Conflict,
            """{ "code": "revision-conflict", "message": "The task changed since it was loaded. Reload it and try again." }"""));

        Assert.Equal("revision-conflict", error.Code);
        Assert.Contains("Reload it", error.Message);
        Assert.True(error.RequiresReload);
    }

    [Fact]
    public void Describe_TreatsAConcealedTaskLikeAMissingOne()
    {
        // The server answers a denied command with 404 and the not-found copy, so the client must not
        // present it as an authorization failure and re-introduce the distinction.
        var error = UserTaskErrorMapper.Describe(CreateApiException(HttpStatusCode.NotFound,
            """{ "code": "forbidden", "message": "This task is no longer available." }"""));

        Assert.Equal("This task is no longer available.", error.Message);
        Assert.False(error.RequiresReload);
        Assert.False(error.IsAuthorization);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, false, true)]
    [InlineData(HttpStatusCode.Unauthorized, false, true)]
    [InlineData(HttpStatusCode.Conflict, true, false)]
    [InlineData(HttpStatusCode.NotFound, false, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false, false)]
    public void Describe_ClassifiesStatusCodesWhenTheBodyIsAbsent(HttpStatusCode statusCode, bool requiresReload, bool isAuthorization)
    {
        var error = UserTaskErrorMapper.Describe(CreateApiException(statusCode, null));

        Assert.Equal(requiresReload, error.RequiresReload);
        Assert.Equal(isAuthorization, error.IsAuthorization);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void Describe_DiscardsANonConformingBodyRatherThanShowingIt()
    {
        var error = UserTaskErrorMapper.Describe(CreateApiException(HttpStatusCode.BadGateway, "<html><body>Proxy error at 10.0.0.5</body></html>"));

        Assert.DoesNotContain("10.0.0.5", error.Message);
        Assert.Equal(UserTaskErrorInfo.Unavailable.Message, error.Message);
    }

    [Fact]
    public void Describe_NeverSurfacesARawExceptionMessage()
    {
        var error = UserTaskErrorMapper.Describe(new InvalidOperationException("Connection string: Server=db;Password=hunter2"));

        Assert.DoesNotContain("hunter2", error.Message);
        Assert.Equal(UserTaskErrorInfo.Unavailable, error);
    }
}
