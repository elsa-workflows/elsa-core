using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedBookmarkQueueWorker : BookmarkQueueWorker
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    private readonly IDistributedLockProvider _distributedLockProvider;

    public DistributedBookmarkQueueWorker(
        IDistributedLockProvider distributedLockProvider,
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        ILogger<DistributedBookmarkQueueWorker> logger) : base(signaler, scopeFactory, logger)
    {
        _distributedLockProvider = distributedLockProvider;
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await using var handle = await _distributedLockProvider.TryAcquireLockAsync(nameof(DistributedBookmarkQueueWorker), TimeSpan.Zero, cancellationToken);

        if (handle == null)
        {
            Logger.LogDebug("Could not acquire lock for distributed bookmark queue worker. This is usually an indication that another application instance is already processing.");
            await Task.Delay(RetryDelay, cancellationToken);
            await Signaler.TriggerAsync(cancellationToken);
            return;
        }

        await base.ProcessAsync(cancellationToken);
    }
}
