using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedBookmarkQueueWorker(
    IDistributedLockProvider distributedLockProvider,
    IBookmarkQueueSignaler signaler,
    IServiceScopeFactory scopeFactory,
    ILogger<DistributedBookmarkQueueWorker> logger) : BookmarkQueueWorker(signaler, scopeFactory, logger)
{
    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        // The current owner may already have passed a stimulus whose bookmark just became available.
        // Preserve this wakeup by waiting for a pass of our own instead of discarding it on contention.
        await using var handle = await distributedLockProvider.AcquireLockAsync(nameof(DistributedBookmarkQueueWorker), cancellationToken: cancellationToken);

        await base.ProcessAsync(cancellationToken);
    }
}