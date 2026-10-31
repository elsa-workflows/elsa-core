using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Periodically signals the bookmark queue processor to check for new items. This is a reliability measure that ensures stimuli never gets missed.
/// </summary>
/// <remarks>
/// Runs once per cycle for all tenants and signals only the tenants that have queue items; items with an empty, null or "*" tenant id signal the default tenant.
/// Every tenant's worker is still started.
/// Tenants with their own databases are not covered.
/// </remarks>
[RunOnceForAllTenants]
[UsedImplicitly]
public class TriggerBookmarkQueueRecurringTask(
    IBookmarkQueueWorker bookmarkQueueWorker,
    IBookmarkQueueSignaler signaler,
    ITenantAccessor? tenantAccessor,
    IBookmarkQueueStore? bookmarkQueueStore,
    ITenantService? tenantService,
    ILogger<TriggerBookmarkQueueRecurringTask>? logger = null) : IRecurringTask
{
    private readonly ILogger _logger = logger ?? NullLogger<TriggerBookmarkQueueRecurringTask>.Instance;
    private Tenant? _tenant;

    /// <summary>
    /// Initializes a new instance that signals the executing tenant only.
    /// </summary>
    [Obsolete("This constructor only signals the executing tenant. Use the constructor that accepts an IBookmarkQueueStore and an ITenantService to signal all tenants. Removed in the next major version.")]
    public TriggerBookmarkQueueRecurringTask(IBookmarkQueueWorker bookmarkQueueWorker, IBookmarkQueueSignaler signaler, ITenantAccessor? tenantAccessor = null)
        : this(bookmarkQueueWorker, signaler, tenantAccessor, null, null)
    {
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _tenant = tenantAccessor?.Tenant;
        bookmarkQueueWorker.Start();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        bookmarkQueueWorker.Stop();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (tenantService is null || tenantAccessor is null)
        {
            using var tenantContext = tenantAccessor?.PushContext(_tenant);
            await signaler.TriggerAsync(stoppingToken);
            return;
        }

        foreach (var tenant in await GetTenantsAsync(tenantService, stoppingToken))
        {
            using (tenantAccessor.PushContext(tenant))
                await signaler.TriggerAsync(stoppingToken);
        }
    }

    private async Task<IReadOnlyCollection<Tenant>> GetTenantsAsync(ITenantService service, CancellationToken cancellationToken)
    {
        var tenantIds = bookmarkQueueStore is null ? null : await bookmarkQueueStore.ListTenantIdsAsync(cancellationToken);

        if (tenantIds is null)
            return (await service.ListAsync(cancellationToken)).DistinctBy(x => x.Id).ToList();

        var tenants = new List<Tenant>();

        foreach (var tenantId in tenantIds.Select(TenantKey.From).Distinct())
        {
            var tenant = await service.FindAsync(tenantId, cancellationToken);

            if (tenant is null)
                _logger.LogDebug("Skipping bookmark queue signal for tenant {TenantId}: the tenant was not found.", tenantId);
            else
                tenants.Add(tenant);
        }

        return tenants;
    }
}
