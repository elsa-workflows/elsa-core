using System.Text.Json;
using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Handlers;
using Elsa.UserTasks.Models;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;
using NSubstitute;
using Xunit;

namespace Elsa.UserTasks.UnitTests;

public class UserTaskBookmarkPersistedHandlerRemovalTests
{
    [Fact]
    public async Task HandleAsync_FinalizesRemovedBookmarkWithCamelCasedPayload()
    {
        var projectionService = Substitute.For<IUserTaskProjectionService>();
        var handler = new UserTaskBookmarkPersistedHandler(projectionService, Substitute.For<ISystemClock>());
        // Shape of a payload read back from storage without a registered type: an untyped, camelCased property bag.
        var payload = new Dictionary<string, object?> { ["tenantId"] = "tenant-1", ["taskId"] = "task-1" };
        var bookmark = new Bookmark("bookmark-1", "UserTask", "hash", payload, "a1", "n1", null, DateTimeOffset.UtcNow);
        var diff = Diff.From<Bookmark>([], [bookmark], []);

        await handler.HandleAsync(new WorkflowBookmarksPersisted(diff), CancellationToken.None);

        await projectionService.Received(1).FinalizeBookmarkRemovalAsync(
            Arg.Is<UserTaskBookmarkRemoval>(x => x.TenantId == "tenant-1" && x.TaskId == "task-1" && x.BookmarkId == "bookmark-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FinalizesRemovedBookmarkWithJsonElementPayloadAndStringEnums()
    {
        var projectionService = Substitute.For<IUserTaskProjectionService>();
        var handler = new UserTaskBookmarkPersistedHandler(projectionService, Substitute.For<ISystemClock>());
        // Full stored payload as read back from storage: camelCased, with enums written as strings.
        const string json = """
            {
              "tenantId": "tenant-1",
              "workflowDefinitionId": "def-1",
              "workflowInstanceId": "wi-1",
              "activityInstanceId": "ai-1",
              "bookmarkId": "bookmark-1",
              "definition": {
                "title": "Approve",
                "assignee": { "tenantId": "tenant-1", "provider": "default", "type": "User", "id": "alice" },
                "candidateGroups": [{ "tenantId": "tenant-1", "provider": "default", "type": "Group", "id": "approvers" }],
                "membershipResolutionMode": "Live"
              },
              "snapshotMembers": [{ "tenantId": "tenant-1", "provider": "default", "type": "User", "id": "alice" }],
              "snapshotGroups": [],
              "createdAt": "2026-10-13T00:00:00+00:00",
              "taskId": "task-1"
            }
            """;
        var payload = JsonDocument.Parse(json).RootElement.Clone();
        var bookmark = new Bookmark("bookmark-1", "UserTask", "hash", payload, "a1", "n1", null, DateTimeOffset.UtcNow);
        var diff = Diff.From<Bookmark>([], [bookmark], []);

        await handler.HandleAsync(new WorkflowBookmarksPersisted(diff), CancellationToken.None);

        await projectionService.Received(1).FinalizeBookmarkRemovalAsync(
            Arg.Is<UserTaskBookmarkRemoval>(x => x.TenantId == "tenant-1" && x.TaskId == "task-1" && x.BookmarkId == "bookmark-1"),
            Arg.Any<CancellationToken>());
    }
}
