using System.Security.Claims;
using System.Text.Json;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Repositories;
using Elsa.UserTasks.Services;
using Xunit;

namespace Elsa.UserTasks.UnitTests;

public class UserTaskTests
{
    private readonly UserTaskTestFixture _fixture = new();

    [Fact]
    public async Task ClaimsResolver_PreservesExternalGroupClaimValues()
    {
        var resolver = new DefaultClaimsIdentityResolver(Microsoft.Extensions.Options.Options.Create(new UserTasksOptions { DefaultTenantId = "tenant", DefaultProvider = "oidc" }));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-1"),
            new Claim("groups", "group,with;delimiters"),
            new Claim("groups", "finance"),
            new Claim("permission", "read:user-tasks")
        ], "test"));

        var actor = await resolver.ResolveAsync(principal);

        Assert.NotNull(actor);
        Assert.Contains(actor.Groups, x => x.Id == "group,with;delimiters");
        Assert.Contains(actor.Groups, x => x.Id == "finance");
        Assert.Equal("tenant", actor.Subject.TenantId);
        Assert.Equal("oidc", actor.Subject.Provider);
    }

    [Fact]
    public async Task Repository_CursorIsStableAndTotalCountIgnoresCursor()
    {
        var repository = new InMemoryUserTaskRepository();
        for (var i = 0; i < 3; i++)
            await repository.AddProjectionAsync(new() { Id = $"task-{i}", TenantId = "tenant", Title = $"Task {i}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i) });

        var first = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, IncludeTotalCount = true });
        var second = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, Cursor = first.NextCursor, IncludeTotalCount = true });

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(3, second.TotalCount);
        Assert.Single(second.Items);
        Assert.DoesNotContain(first.Items.Select(x => x.Id), x => second.Items.Any(y => y.Id == x));
    }

    [Theory]
    [InlineData("created", false)]
    [InlineData("created", true)]
    [InlineData("due", false)]
    [InlineData("due", true)]
    [InlineData("priority", false)]
    [InlineData("priority", true)]
    [InlineData("title", false)]
    [InlineData("title", true)]
    public async Task Repository_CursorCoversSupportedSortsAndDirections(string sort, bool descending)
    {
        var repository = new InMemoryUserTaskRepository();
        var now = DateTimeOffset.UtcNow;
        await repository.AddProjectionAsync(new() { Id = "task-a", TenantId = "tenant", Title = "Alpha", Priority = 10, DueAt = now.AddHours(1), CreatedAt = now.AddMinutes(1) });
        await repository.AddProjectionAsync(new() { Id = "task-b", TenantId = "tenant", Title = "Beta", Priority = 50, DueAt = null, CreatedAt = now.AddMinutes(2) });
        await repository.AddProjectionAsync(new() { Id = "task-c", TenantId = "tenant", Title = "Gamma", Priority = 90, DueAt = now.AddHours(2), CreatedAt = now.AddMinutes(3) });

        var first = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, Sort = sort, Descending = descending, IncludeTotalCount = true });
        var second = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, Sort = sort, Descending = descending, Cursor = first.NextCursor, IncludeTotalCount = true });
        var ids = first.Items.Concat(second.Items).Select(x => x.Id).ToArray();

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(3, second.TotalCount);
        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task Manager_HidesProtectedFieldsUntilClaimAndCompletesAfterBookmarkFinalization()
    {
        var actor = _fixture.Actor("user-1");
        var task = await _fixture.ProjectAsync(actor.Subject);

        var candidateDetail = await _fixture.Manager.GetAsync(UserTaskTestFixture.TenantId, task.Id, actor);
        Assert.NotNull(candidateDetail);
        Assert.Null(candidateDetail.Instructions);
        Assert.False(candidateDetail.Disclosure.CanViewProtected);

        var claimed = await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), actor);
        Assert.True(claimed.Accepted);
        var assignedDetail = await _fixture.Manager.GetAsync(UserTaskTestFixture.TenantId, task.Id, actor);
        Assert.Equal("private instructions", assignedDetail!.Instructions);
        Assert.True(assignedDetail.Disclosure.CanViewProtected);
        Assert.NotEmpty(assignedDetail.Actions);

        var completing = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.True(completing.Accepted);
        Assert.Equal(UserTaskStatus.Completing, completing.Task.Status);
        Assert.Equal(actor.Subject, _fixture.Resumer.LastStimulus!.CompletedBy);

        var retry = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.True(retry.Accepted);
        Assert.Equal(completing.Operation.OperationId, retry.Operation.OperationId);

        var divergent = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Reject"), actor);
        Assert.False(divergent.Accepted);
        Assert.Equal("idempotency-conflict", divergent.ConflictCode);

        await _fixture.Projection.FinalizeBookmarkRemovalAsync(new(UserTaskTestFixture.TenantId, task.Id, task.BookmarkId, _fixture.Clock.UtcNow));
        var completed = await _fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        Assert.Equal(UserTaskStatus.Completed, completed!.Status);

        var terminalRetry = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.True(terminalRetry.Accepted);
        Assert.Equal(completing.Operation.OperationId, terminalRetry.Operation.OperationId);
    }

    [Fact]
    public async Task Policy_RequiresOperationPermissionInAdditionToCandidateRelationship()
    {
        var actor = _fixture.Actor("user-1", "read:user-tasks");
        var task = await _fixture.ProjectAsync(actor.Subject);

        var claim = await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-no-permission"), actor);

        Assert.False(claim.Accepted);
        Assert.Equal("forbidden", claim.ConflictCode);
    }

    [Fact]
    public async Task Policy_ManagerFlagAloneDoesNotGrantManagementWithoutThePermission()
    {
        var actor = _fixture.Actor("user-1", "read:user-tasks", "assign:user-tasks") with { IsManager = true };
        var candidate = _fixture.Actor("user-2");
        var task = await _fixture.ProjectAsync(candidate.Subject);

        // The host set IsManager but never granted manage:user-tasks, so management must still be refused.
        Assert.False(await _fixture.Policy.AuthorizeAsync(task, actor, UserTaskAccessOperation.Assign));
        Assert.Null(await _fixture.Policy.CreateScopeAsync(actor, UserTaskQueryScopeKind.All));
    }

    [Fact]
    public async Task Policy_WildcardPermissionGrantSatisfiesManagementChecks()
    {
        var root = _fixture.Actor("root", "*") with { IsManager = true };
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject);

        Assert.True(await _fixture.Policy.AuthorizeAsync(task, root, UserTaskAccessOperation.Manage));
        var scope = await _fixture.Policy.CreateScopeAsync(root, UserTaskQueryScopeKind.All);
        Assert.NotNull(scope);
        Assert.True(scope.IsManager);
    }

    [Theory]
    [InlineData(UserTaskQueryScopeKind.All)]
    [InlineData(UserTaskQueryScopeKind.NeedsAttention)]
    public async Task Query_ManagerOnlyScopesAreDeniedRatherThanSilentlyNarrowed(UserTaskQueryScopeKind kind)
    {
        var actor = _fixture.Actor("user-1");
        await _fixture.ProjectAsync(actor.Subject);

        // A denial must be distinguishable from an empty page, or the endpoint would answer 200 with no rows.
        Assert.Null(await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, kind, actor));
    }

    [Fact]
    public async Task Query_AvailableScopeExcludesClaimedTasksAndExcludedCandidates()
    {
        var candidate = _fixture.Actor("user-1");
        var excluded = _fixture.Actor("user-2");
        var open = await _fixture.ProjectAsync(candidate.Subject, definition => definition with
        {
            CandidateUsers = [candidate.Subject, excluded.Subject],
            ExcludedUsers = [excluded.Subject]
        });

        var availableToCandidate = await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Available, candidate);
        Assert.NotNull(availableToCandidate);
        Assert.Single(availableToCandidate.Items);

        var availableToExcluded = await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Available, excluded);
        Assert.NotNull(availableToExcluded);
        Assert.Empty(availableToExcluded.Items);

        await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, open.Id, new(1, "claim-1"), candidate);

        var afterClaim = await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Available, candidate);
        Assert.NotNull(afterClaim);
        Assert.Empty(afterClaim.Items);

        var assigned = await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Assigned, candidate);
        Assert.NotNull(assigned);
        Assert.Single(assigned.Items);
    }

    [Fact]
    public async Task Summary_DoesNotDiscloseCandidateIdentitiesOrBlockingHealthToParticipants()
    {
        var candidate = _fixture.Actor("user-1");
        var peer = _fixture.Actor("user-2");
        var task = await _fixture.ProjectAsync(candidate.Subject, definition => definition with { CandidateUsers = [candidate.Subject, peer.Subject] });
        task.HealthSeverity = UserTaskHealthSeverity.Advisory;
        task.HealthCode = "advisory-code";

        var summary = await UserTaskModelMapper.ToSummaryAsync(task, candidate, _fixture.Policy);

        Assert.Equal("2 users", summary.CandidateSummary);
        Assert.DoesNotContain(peer.Subject.Id, JsonSerializer.Serialize(summary));
        Assert.Null(summary.HealthSeverity);
        Assert.Null(summary.HealthCode);
    }

    [Fact]
    public async Task Summary_CarriesWorkflowContextForAuthorizedParticipants()
    {
        var actor = _fixture.Actor("user-1");
        var task = await _fixture.ProjectAsync(actor.Subject);

        var summary = await UserTaskModelMapper.ToSummaryAsync(task, actor, _fixture.Policy);

        Assert.Equal("Approval workflow", summary.WorkflowDefinitionName);
        Assert.Equal(3, summary.WorkflowDefinitionVersion);
        Assert.Equal("correlation-1", summary.WorkflowInstanceReference);
    }

    [Fact]
    public async Task Events_AreWithheldFromParticipantsWithoutProtectedAccess()
    {
        var candidate = _fixture.Actor("user-1");
        var task = await _fixture.ProjectAsync(candidate.Subject);

        var beforeClaim = await _fixture.Manager.GetEventsAsync(UserTaskTestFixture.TenantId, task.Id, null, 50, candidate);
        Assert.NotNull(beforeClaim);
        Assert.Empty(beforeClaim.Items);

        await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), candidate);

        var afterClaim = await _fixture.Manager.GetEventsAsync(UserTaskTestFixture.TenantId, task.Id, null, 50, candidate);
        Assert.NotNull(afterClaim);
        Assert.NotEmpty(afterClaim.Items);
        // Actor identifiers never reach the audit projection; only a display name may.
        Assert.DoesNotContain(candidate.Subject.Id, JsonSerializer.Serialize(afterClaim));
    }

    [Fact]
    public async Task Detail_WithholdsMaskedFieldValuesUntilAnExplicitRevealAndDoesNotConsumeTheRevision()
    {
        var form = new TestFormProvider(
            new("note", "Note"),
            new("iban", "IBAN", Masked: true, CanReveal: true),
            new("pin", "PIN", Masked: true));
        var fixture = new UserTaskTestFixture(formProviders: form);
        var actor = fixture.Actor("user-1");
        var payload = JsonDocument.Parse("""{"note":"visible","iban":"NL00BANK","pin":"1234"}""").RootElement;
        var task = await fixture.ProjectAsync(actor.Subject, definition => definition with
        {
            FormReference = new("test", "invoice"),
            TaskData = payload
        });
        await fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), actor);

        var detail = await fixture.Manager.GetAsync(UserTaskTestFixture.TenantId, task.Id, actor);
        var fields = detail!.Form!.Fields.ToDictionary(x => x.Key);
        Assert.Equal("visible", fields["note"].Value?.GetString());
        Assert.Null(fields["iban"].Value);
        Assert.Null(fields["pin"].Value);
        Assert.True(fields["iban"].CanReveal);
        Assert.False(fields["pin"].CanReveal);
        Assert.DoesNotContain("NL00BANK", JsonSerializer.Serialize(detail.Form));

        var revisionBeforeReveal = detail.Revision;
        var revealed = await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "iban", actor);
        Assert.Equal("NL00BANK", revealed?.GetString());

        // A field the provider did not mark revealable is indistinguishable from an unknown one.
        Assert.Null(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "pin", actor));
        Assert.Null(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "nope", actor));

        var afterReveal = await fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        // The reveal is audited but must not consume the concurrency token, or the caller's next command
        // would conflict for no reason.
        Assert.Equal(revisionBeforeReveal, afterReveal!.Revision);
        Assert.Contains(afterReveal.Events, x => x.EventType == "FieldRevealed");
        Assert.DoesNotContain("NL00BANK", JsonSerializer.Serialize(afterReveal.Events));

        var completion = await fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id,
            new(revisionBeforeReveal, "complete-1", "Approve", payload), actor);
        Assert.True(completion.Accepted);
    }

    [Fact]
    public async Task RevealField_IsRefusedForCallersWithoutProtectedAccess()
    {
        var form = new TestFormProvider(new UserTaskFormFieldDescriptor("iban", "IBAN", Masked: true, CanReveal: true));
        var fixture = new UserTaskTestFixture(formProviders: form);
        var candidate = fixture.Actor("user-1");
        var task = await fixture.ProjectAsync(candidate.Subject, definition => definition with
        {
            FormReference = new("test", "invoice"),
            TaskData = JsonDocument.Parse("""{"iban":"NL00BANK"}""").RootElement
        });

        // The candidate can see the task but has not claimed it, so protected access is not granted yet.
        Assert.Null(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "iban", candidate));

        await fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), candidate);
        Assert.NotNull(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "iban", candidate));
    }

    [Fact]
    public async Task DueService_ReservesTimeoutAndResumesWorkflow()
    {
        var dueAt = _fixture.Clock.UtcNow.AddMinutes(-1);
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject,
            definition => definition with { DueAt = dueAt, EnableTimeoutOutcome = true });
        var due = new DefaultUserTaskDueService(_fixture.Repository, _fixture.Manager, _fixture.Sink, _fixture.Identity, _fixture.Clock);

        Assert.Equal(1, await due.MarkOverdueAsync(UserTaskTestFixture.TenantId, _fixture.Clock.UtcNow));
        var timingOut = await _fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        Assert.Equal(UserTaskStatus.TimingOut, timingOut!.Status);
        Assert.Equal("Timeout", _fixture.Resumer.LastStimulus!.ActionKey);

        await _fixture.Projection.FinalizeBookmarkRemovalAsync(new(UserTaskTestFixture.TenantId, task.Id, task.BookmarkId, _fixture.Clock.UtcNow));
        var timedOut = await _fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        Assert.Equal(UserTaskStatus.TimedOut, timedOut!.Status);
    }
}
