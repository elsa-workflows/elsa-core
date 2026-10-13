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
}
