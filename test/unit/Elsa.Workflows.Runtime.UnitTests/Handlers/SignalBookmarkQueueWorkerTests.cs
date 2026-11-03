using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Notifications;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class SignalBookmarkQueueWorkerTests
{
    /// <summary>
    /// Generic workflow saves and pre-state-persistence indexing do not request queue processing.
    /// </summary>
    [Theory]
    [InlineData("save")]
    [InlineData("empty-index")]
    [InlineData("removed-index")]
    [InlineData("unchanged-index")]
    [InlineData("added-index")]
    public async Task HandleAsync_IgnoresUnrelatedAndPrematureNotifications(string kind)
    {
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        var handler = new SignalBookmarkQueueWorker(signaler);
        if (kind == "save")
            await handler.HandleAsync(new WorkflowInstanceSaved(new WorkflowInstance()), default);
        else
            await handler.HandleAsync(new WorkflowBookmarksIndexed(new(null!,
                kind == "added-index" ? [new Bookmark()] : [],
                kind == "removed-index" ? [new Bookmark()] : [],
                kind == "unchanged-index" ? [new Bookmark()] : [])), default);

        await signaler.DidNotReceiveWithAnyArgs().TriggerAsync(default);
    }

    /// <summary>
    /// Post-save bookmark changes wake the queue only when they introduce a bookmark that can accept delivery.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(1, 0, 0)]
    public async Task HandleAsync_SignalsOnlyAddedCommittedBookmarks(int added, int removed, int unchanged)
    {
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        var handler = new SignalBookmarkQueueWorker(signaler);
        var notification = new WorkflowBookmarksCommitted(Diff.From(
            Enumerable.Range(0, added).Select(_ => new Bookmark()).ToArray(),
            Enumerable.Range(0, removed).Select(_ => new Bookmark()).ToArray(),
            Enumerable.Range(0, unchanged).Select(_ => new Bookmark()).ToArray()));
        using var cancellation = new CancellationTokenSource();

        await handler.HandleAsync(notification, cancellation.Token);

        if (added > 0)
            await signaler.Received(1).TriggerAsync(cancellation.Token);
        else
            await signaler.DidNotReceiveWithAnyArgs().TriggerAsync(default);
    }

    /// <summary>
    /// Explicit bookmark saves preserve their wakeup and the caller's cancellation token.
    /// </summary>
    [Fact]
    public async Task HandleAsync_BookmarkSaved_ForwardsCancellation()
    {
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        var handler = new SignalBookmarkQueueWorker(signaler);
        using var cancellation = new CancellationTokenSource();

        await handler.HandleAsync(new BookmarkSaved(new StoredBookmark()), cancellation.Token);

        await signaler.Received(1).TriggerAsync(cancellation.Token);
    }

    /// <summary>
    /// Signaling failures propagate instead of being reported as successful notification handling.
    /// </summary>
    [Fact]
    public async Task HandleAsync_PropagatesSignalingFailure()
    {
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        var failure = new InvalidOperationException("Controlled signal failure");
        signaler.TriggerAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(failure));
        var handler = new SignalBookmarkQueueWorker(signaler);

        var observed = await Record.ExceptionAsync(() => handler.HandleAsync(new BookmarkSaved(new StoredBookmark()), default));

        Assert.Same(failure, observed);
    }
}
