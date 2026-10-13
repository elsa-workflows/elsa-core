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

public class UserTaskBookmarkPersistedHandlerTests
{
    [Fact]
    public async Task HandleAsync_IgnoresBookmarksFromOtherActivities()
    {
        var projectionService = Substitute.For<IUserTaskProjectionService>();
        var handler = new UserTaskBookmarkPersistedHandler(projectionService, Substitute.For<ISystemClock>());
        var cronBookmark = new Bookmark("b1", "Elsa.Cron", "hash", new { CronExpression = "* * * * *" }, "a1", "n1", null, DateTimeOffset.UtcNow);
        var diff = Diff.From<Bookmark>([cronBookmark], [cronBookmark], []);

        await handler.HandleAsync(new WorkflowBookmarksPersisted(diff), CancellationToken.None);

        await projectionService.DidNotReceiveWithAnyArgs().ProjectCommittedBookmarksAsync(default!, default);
        await projectionService.DidNotReceiveWithAnyArgs().FinalizeBookmarkRemovalAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ProjectsUserTaskBookmarksAndIgnoresOthers()
    {
        var projectionService = Substitute.For<IUserTaskProjectionService>();
        var handler = new UserTaskBookmarkPersistedHandler(projectionService, Substitute.For<ISystemClock>());
        var materialization = new UserTaskMaterialization("tenant", "def", "wi1", "a1", "b1", null!, [], [], DateTimeOffset.UtcNow, TaskId: "task1");
        var userTaskBookmark = new Bookmark("b1", "UserTask", "hash", materialization, "a1", "n1", null, DateTimeOffset.UtcNow);
        var cronBookmark = new Bookmark("b2", "Elsa.Cron", "hash", new { CronExpression = "* * * * *" }, "a2", "n2", null, DateTimeOffset.UtcNow);
        var diff = Diff.From<Bookmark>([userTaskBookmark, cronBookmark], [userTaskBookmark, cronBookmark], []);

        await handler.HandleAsync(new WorkflowBookmarksPersisted(diff), CancellationToken.None);

        await projectionService.Received(1).ProjectCommittedBookmarksAsync(
            Arg.Is<IReadOnlyCollection<UserTaskMaterialization>>(x => x.Count == 1 && x.Single().BookmarkId == "b1"),
            Arg.Any<CancellationToken>());
        await projectionService.Received(1).FinalizeBookmarkRemovalAsync(
            Arg.Is<UserTaskBookmarkRemoval>(x => x.TaskId == "task1" && x.BookmarkId == "b1"),
            Arg.Any<CancellationToken>());
    }
}
