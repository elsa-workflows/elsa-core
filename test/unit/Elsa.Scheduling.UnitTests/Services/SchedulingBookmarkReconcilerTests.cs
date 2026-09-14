using Elsa.Common.Services;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.Services;

public class SchedulingBookmarkReconcilerTests
{
    [Fact]
    public async Task ClassifyAsync_SkipsMissingBlankAndFinishedInstances()
    {
        var instanceStore = CreateInstanceStore(
            Instance("running", WorkflowStatus.Running, WorkflowSubStatus.Executing),
            Instance("suspended", WorkflowStatus.Running, WorkflowSubStatus.Suspended),
            Instance("interrupted", WorkflowStatus.Running, WorkflowSubStatus.Interrupted),
            Instance("pending", WorkflowStatus.Running, WorkflowSubStatus.Pending),
            Instance("finished", WorkflowStatus.Finished, WorkflowSubStatus.Finished),
            Instance("cancelled", WorkflowStatus.Finished, WorkflowSubStatus.Cancelled),
            Instance("faulted", WorkflowStatus.Finished, WorkflowSubStatus.Faulted));
        var reconciler = new SchedulingBookmarkReconciler(instanceStore, Substitute.For<IBookmarkManager>());

        var result = await reconciler.ClassifyAsync(
        [
            Bookmark("b-missing", "missing"),
            Bookmark("b-blank", ""),
            Bookmark("b-finished", "finished"),
            Bookmark("b-cancelled", "cancelled"),
            Bookmark("b-faulted", "faulted"),
            Bookmark("b-running", "running"),
            Bookmark("b-suspended", "suspended"),
            Bookmark("b-interrupted", "interrupted"),
            Bookmark("b-pending", "pending")
        ]);

        Assert.Equal(["b-running", "b-suspended", "b-interrupted", "b-pending"], result.Schedulable.Select(x => x.Id));
        Assert.Equal(["b-missing", "b-blank", "b-finished", "b-cancelled", "b-faulted"], result.Orphans.Select(x => x.Id));
    }

    [Fact]
    public async Task ClassifyAsync_KeepsPastDueSuspendedDelayBookmarks()
    {
        var instanceStore = CreateInstanceStore(Instance("suspended", WorkflowStatus.Running, WorkflowSubStatus.Suspended));
        var reconciler = new SchedulingBookmarkReconciler(instanceStore, Substitute.For<IBookmarkManager>());

        var result = await reconciler.ClassifyAsync([Bookmark("past-due-delay", "suspended")]);

        Assert.Equal(["past-due-delay"], result.Schedulable.Select(x => x.Id));
        Assert.Empty(result.Orphans);
    }

    [Fact]
    public async Task PurgeAsync_DeletesOrphanBookmarksFromTheStore()
    {
        var bookmarkStore = new MemoryBookmarkStore(new MemoryStore<StoredBookmark>());
        await bookmarkStore.SaveManyAsync(
        [
            Bookmark("orphan-1", "missing"),
            Bookmark("orphan-2", "finished"),
            Bookmark("live", "suspended")
        ], CancellationToken.None);
        var bookmarkManager = new DefaultBookmarkManager(bookmarkStore, Substitute.For<INotificationSender>(), NullLogger<DefaultBookmarkManager>.Instance);
        var reconciler = new SchedulingBookmarkReconciler(CreateInstanceStore(), bookmarkManager);

        await reconciler.PurgeAsync([Bookmark("orphan-1", "missing"), Bookmark("orphan-2", "finished")]);

        var remaining = (await bookmarkStore.FindManyAsync(new BookmarkFilter())).Select(x => x.Id).ToList();
        Assert.Equal(["live"], remaining);
    }

    [Fact]
    public async Task PurgeAsync_DoesNotDeleteWhenThereAreNoOrphans()
    {
        var bookmarkManager = Substitute.For<IBookmarkManager>();
        var reconciler = new SchedulingBookmarkReconciler(CreateInstanceStore(), bookmarkManager);

        await reconciler.PurgeAsync([]);

        await bookmarkManager.DidNotReceive().DeleteManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<CancellationToken>());
    }

    private static MemoryWorkflowInstanceStore CreateInstanceStore(params WorkflowInstance[] instances)
    {
        var store = new MemoryStore<WorkflowInstance>();
        store.AddMany(instances, x => x.Id);
        return new MemoryWorkflowInstanceStore(store);
    }

    private static WorkflowInstance Instance(string id, WorkflowStatus status, WorkflowSubStatus subStatus) => new()
    {
        Id = id,
        DefinitionId = "definition",
        DefinitionVersionId = "version",
        Status = status,
        SubStatus = subStatus
    };

    private static StoredBookmark Bookmark(string id, string workflowInstanceId) => new()
    {
        Id = id,
        Hash = "hash",
        Name = SchedulingStimulusNames.Delay,
        WorkflowInstanceId = workflowInstanceId
    };
}
