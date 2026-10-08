using Elsa.Common.DistributedHosting;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Stores;
using Medallion.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

/// <summary>
/// Rolling-upgrade overlap: an old node still locks <c>DistributedBookmarkQueueWorker</c>
/// while a new node locks <c>DistributedBookmarkQueueWorker:{tenantId}</c>, so the same queue
/// item can be processed twice. After the first resume consumes the bookmark, a second resume
/// must be a no-op.
/// </summary>
public class WorkflowResumerConsumedBookmarkTests
{
    [Fact]
    public async Task ResumeAsync_WhenBookmarkAlreadyConsumed_IsNoOp()
    {
        var store = new MemoryBookmarkStore(new MemoryStore<StoredBookmark>());
        await store.SaveAsync(new StoredBookmark
        {
            Id = "bookmark-1",
            WorkflowInstanceId = "instance-1",
            Hash = "hash",
            Name = "Elsa.Event"
        });

        var runCount = 0;
        var client = Substitute.For<IWorkflowClient>();
        client.RunInstanceAsync(Arg.Any<RunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                runCount++;
                return new RunWorkflowInstanceResponse { WorkflowInstanceId = "instance-1" };
            });

        var runtime = Substitute.For<IWorkflowRuntime>();
        runtime.CreateClientAsync("instance-1", Arg.Any<CancellationToken>()).Returns(client);

        var lockProvider = Substitute.For<IDistributedLockProvider>();
        var distributedLock = Substitute.For<IDistributedLock>();
        var lockHandle = Substitute.For<IDistributedSynchronizationHandle>();
        lockProvider.CreateLock(Arg.Any<string>()).Returns(distributedLock);
        distributedLock.AcquireAsync(Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(lockHandle));

        var resumer = new WorkflowResumer(
            runtime,
            store,
            Substitute.For<IStimulusHasher>(),
            lockProvider,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            NullLogger<WorkflowResumer>.Instance);
        var filter = new BookmarkFilter { BookmarkId = "bookmark-1" };

        var first = (await resumer.ResumeAsync(filter)).ToList();
        Assert.Single(first);
        Assert.Equal(1, runCount);

        await store.DeleteAsync(new BookmarkFilter { BookmarkId = "bookmark-1" });

        var second = (await resumer.ResumeAsync(filter)).ToList();
        Assert.Empty(second);
        Assert.Equal(1, runCount);
    }
}
