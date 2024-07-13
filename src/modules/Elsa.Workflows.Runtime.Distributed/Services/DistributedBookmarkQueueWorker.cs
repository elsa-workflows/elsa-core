using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedBookmarkQueueWorker(
    IDistributedLockProvider distributedLockProvider,
    IBookmarkQueueWorkerSignaler signaler,
    IBookmarkQueueProcessor processor,
    ILogger<DistributedBookmarkQueueWorker> logger) : BookmarkQueueWorker(signaler, processor)
{
    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await using var handle = await distributedLockProvider.TryAcquireLockAsync(nameof(DistributedBookmarkQueueWorker), default, cancellationToken);

        if (handle == null)
        {
            logger.LogInformation("Could not acquire lock for distributed bookmark queue worker. This is usually an indication that another application instance is already processing.");
            return;
        }

        await base.ProcessAsync(cancellationToken);
    }
}