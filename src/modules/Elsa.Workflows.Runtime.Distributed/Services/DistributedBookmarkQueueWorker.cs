using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedBookmarkQueueWorker : BookmarkQueueWorker
{
    private static readonly TimeSpan LockUnavailableRetryDelay = TimeSpan.FromSeconds(1);
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly IBookmarkQueueSignaler _signaler;
    private readonly ILogger<DistributedBookmarkQueueWorker> _logger;

    public DistributedBookmarkQueueWorker(
        IDistributedLockProvider distributedLockProvider,
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        ILogger<DistributedBookmarkQueueWorker> logger) : base(signaler, scopeFactory, logger)
    {
        _distributedLockProvider = distributedLockProvider;
        _signaler = signaler;
        _logger = logger;
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await using var handle = await _distributedLockProvider.TryAcquireLockAsync(nameof(DistributedBookmarkQueueWorker), TimeSpan.Zero, cancellationToken);

        if (handle == null)
        {
            _logger.LogDebug("Could not acquire lock for distributed bookmark queue worker. This is usually an indication that another application instance is already processing.");
            await Task.Delay(LockUnavailableRetryDelay, cancellationToken);
            await _signaler.TriggerAsync(cancellationToken);
            return;
        }

        await base.ProcessAsync(cancellationToken);
    }
}
