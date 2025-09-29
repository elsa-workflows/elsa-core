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
        await using var handle = await distributedLockProvider.TryAcquireLockAsync(nameof(DistributedBookmarkQueueWorker), TimeSpan.Zero, cancellationToken);

        if (handle == null)
        {
            logger.LogInformation("Could not acquire lock for distributed bookmark queue worker. This is usually an indication that another application instance is already processing.");
            return;
        }

        await base.ProcessAsync(cancellationToken);
    }
}