using Elsa.Common.Multitenancy;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedBookmarkQueueWorker : BookmarkQueueWorker
{
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly ILogger<DistributedBookmarkQueueWorker> _logger;

    public DistributedBookmarkQueueWorker(
        IDistributedLockProvider distributedLockProvider,
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        ILogger<DistributedBookmarkQueueWorker> logger,
        ITenantScopeFactory? tenantScopeFactory = null,
        ITenantAccessor? tenantAccessor = null)
        : this(distributedLockProvider, signaler, scopeFactory, logger, tenantScopeFactory, tenantAccessor, TimeSpan.FromMilliseconds(500))
    {
    }

    protected DistributedBookmarkQueueWorker(
        IDistributedLockProvider distributedLockProvider,
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        ILogger<DistributedBookmarkQueueWorker> logger,
        ITenantScopeFactory? tenantScopeFactory,
        ITenantAccessor? tenantAccessor,
        TimeSpan processThrottle)
        : base(signaler, scopeFactory, logger, tenantScopeFactory, tenantAccessor, processThrottle)
    {
        _distributedLockProvider = distributedLockProvider;
        _logger = logger;
    }

    internal static string GetLockName(string? tenantId)
    {
        var normalized = tenantId.NormalizeTenantId();
        return string.IsNullOrEmpty(normalized)
            ? nameof(DistributedBookmarkQueueWorker)
            : $"{nameof(DistributedBookmarkQueueWorker)}:{normalized}";
    }

    protected virtual TimeSpan LockRetryDelay => TimeSpan.FromSeconds(2);

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var lockName = GetLockName(CapturedTenantId);
        await using var handle = await _distributedLockProvider.TryAcquireLockAsync(lockName, TimeSpan.Zero, cancellationToken);

        if (handle == null)
        {
            _logger.LogDebug("Could not acquire lock for distributed bookmark queue worker. Another application instance is already processing; scheduling a local retry in {RetryDelay}.", LockRetryDelay);
            await Task.Delay(LockRetryDelay, cancellationToken);
            if (Signaler is BookmarkQueueSignaler bookmarkQueueSignaler)
                await bookmarkQueueSignaler.TriggerAsync(CapturedTenantId, cancellationToken);
            else
                await Signaler.TriggerAsync(cancellationToken);
            return;
        }

        await base.ProcessAsync(cancellationToken);
    }
}
