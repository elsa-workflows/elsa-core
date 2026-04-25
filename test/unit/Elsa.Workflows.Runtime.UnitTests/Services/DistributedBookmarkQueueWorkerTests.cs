using Elsa.Workflows.Runtime.Distributed;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DistributedBookmarkQueueWorkerTests
{
    [Fact(DisplayName = "ProcessAsync re-signals when distributed lock is unavailable")]
    public async Task ProcessAsync_LockUnavailable_TriggersRetrySignal()
    {
        var distributedLockProvider = new UnavailableDistributedLockProvider();
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var logger = Substitute.For<ILogger<DistributedBookmarkQueueWorker>>();
        var worker = new TestDistributedBookmarkQueueWorker(distributedLockProvider, signaler, scopeFactory, logger);

        await worker.ProcessOnceAsync(CancellationToken.None);

        await signaler.Received(1).TriggerAsync(Arg.Any<CancellationToken>());
        scopeFactory.DidNotReceive().CreateScope();
    }

    private class TestDistributedBookmarkQueueWorker(
        IDistributedLockProvider distributedLockProvider,
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        ILogger<DistributedBookmarkQueueWorker> logger) : DistributedBookmarkQueueWorker(distributedLockProvider, signaler, scopeFactory, logger)
    {
        public Task ProcessOnceAsync(CancellationToken cancellationToken) => ProcessAsync(cancellationToken);
    }

    private class UnavailableDistributedLockProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new UnavailableDistributedLock(name);
    }

    private class UnavailableDistributedLock(string name) : IDistributedLock
    {
        public string Name { get; } = name;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) => null;

        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) => ValueTask.FromResult<IDistributedSynchronizationHandle?>(null);
    }
}
