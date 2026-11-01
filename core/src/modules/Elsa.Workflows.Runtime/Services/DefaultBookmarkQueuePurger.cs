using Elsa.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Dead-letters and deletes expired bookmark queue items, then deletes expired dead-letter items.
/// </summary>
/// <remarks>
/// When <see cref="ITenantService"/> and <see cref="ITenantAccessor"/> are available, one pass covers all tenants: each batch is read tenant-agnostic and written under the tenant of the item.
/// Items of tenants that <see cref="ITenantService"/> cannot find are purged under a synthetic tenant with the same id, because their TTL has already elapsed.
/// Items that could not be deleted are left in the store and skipped by the rest of the pass.
/// A tenant whose items live in its own database (a connection string in its configuration) is purged separately in its own scope.
/// </remarks>
[UsedImplicitly]
public class DefaultBookmarkQueuePurger(
    IBookmarkQueueStore store,
    IBookmarkQueueDeadLetterStore deadLetterStore,
    IBookmarkQueueDeadLetterManager deadLetterManager,
    ISystemClock systemClock,
    IOptions<BookmarkQueuePurgeOptions> options,
    ILogger<DefaultBookmarkQueuePurger> logger,
    ITenantService? tenantService,
    ITenantAccessor? tenantAccessor,
    ITenantScopeFactory? tenantScopeFactory = null) : IBookmarkQueuePurger
{
    /// <summary>
    /// Initializes a new instance that purges the executing tenant only.
    /// </summary>
    [Obsolete("This constructor only purges the executing tenant. Use the constructor that accepts an ITenantService, an ITenantAccessor and an ITenantScopeFactory to purge all tenants. Removed in the next major version.")]
    public DefaultBookmarkQueuePurger(
        IBookmarkQueueStore store,
        IBookmarkQueueDeadLetterStore deadLetterStore,
        IBookmarkQueueDeadLetterManager deadLetterManager,
        ISystemClock systemClock,
        IOptions<BookmarkQueuePurgeOptions> options,
        ILogger<DefaultBookmarkQueuePurger> logger)
        : this(store, deadLetterStore, deadLetterManager, systemClock, options, logger, null, null)
    {
    }

    private bool IsMultitenant => tenantService is not null && tenantAccessor is not null;

    /// <inheritdoc />
    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        var ownDatabaseTenants = IsMultitenant
            ? await OwnDatabaseTenants.TryListAsync(tenantService, tenantScopeFactory, logger, cancellationToken)
            : [];

        try
        {
            await PurgeSharedAsync(ownDatabaseTenants, cancellationToken);
        }
        catch (Exception ex) when (ownDatabaseTenants.Count > 0 && OwnDatabaseTenants.IsRecoverable(ex, cancellationToken))
        {
            logger.LogError(ex, "Failed to purge the shared bookmark queue; tenants with their own database are still purged.");
        }

        if (ownDatabaseTenants.Count == 0)
            return;

        logger.LogInformation("Purging the bookmark queue separately for {Count} tenant(s) with their own database.", ownDatabaseTenants.Count);
        logger.LogDebug("Tenants with their own database: {TenantIds}", string.Join(", ", ownDatabaseTenants.Select(x => x.Id)));

        foreach (var tenant in ownDatabaseTenants)
        {
            try
            {
                await using var scope = tenantScopeFactory!.CreateScope(tenant);
                await CreateScopedPurger(scope.ServiceProvider, multitenant: false).PurgeCoreAsync(cancellationToken);
            }
            catch (Exception ex) when (OwnDatabaseTenants.IsRecoverable(ex, cancellationToken))
            {
                logger.LogError(ex, "Failed to purge the bookmark queue of tenant {TenantId} from its own database; it will be retried by the next pass.", tenant.Id);
            }
        }
    }

    private async Task PurgeSharedAsync(IReadOnlyCollection<Tenant> ownDatabaseTenants, CancellationToken cancellationToken)
    {
        if (!OwnDatabaseTenants.NeedsSharedScope(ownDatabaseTenants, tenantAccessor))
        {
            await PurgeCoreAsync(cancellationToken);
            return;
        }

        await using var sharedScope = tenantScopeFactory!.CreateScope(Tenant.Default);
        await CreateScopedPurger(sharedScope.ServiceProvider, multitenant: true).PurgeCoreAsync(cancellationToken);
    }

    private DefaultBookmarkQueuePurger CreateScopedPurger(IServiceProvider services, bool multitenant) => new(
        services.GetRequiredService<IBookmarkQueueStore>(),
        services.GetRequiredService<IBookmarkQueueDeadLetterStore>(),
        services.GetRequiredService<IBookmarkQueueDeadLetterManager>(),
        systemClock,
        options,
        logger,
        multitenant ? tenantService : null,
        multitenant ? tenantAccessor : null);

    private async Task PurgeCoreAsync(CancellationToken cancellationToken)
    {
        var now = systemClock.UtcNow;
        var tenants = new Dictionary<string, Tenant?>();

        try
        {
            await PurgeItemsAsync(now - options.Value.Ttl, tenants, cancellationToken);
        }
        catch (Exception ex) when (IsIsolatable(ex, cancellationToken))
        {
            logger.LogError(ex, "Failed to purge expired bookmark queue items; continuing with the dead-letter items.");
        }

        await PurgeDeadLettersAsync(now, tenants, cancellationToken);

        logger.LogDebug("Finished purging bookmark queue items.");
    }

    private async Task PurgeItemsAsync(DateTimeOffset thresholdDate, Dictionary<string, Tenant?> tenants, CancellationToken cancellationToken)
    {
        var attemptedIds = new HashSet<string>();
        var leftBehindCount = 0;

        logger.LogDebug("Purging bookmark queue items older than {ThresholdDate}.", thresholdDate);

        while (true)
        {
            var pageArgs = PageArgs.FromRange(leftBehindCount, options.Value.BatchSize);
            var filter = new BookmarkQueueFilter
            {
                CreatedAtLessThan = thresholdDate,
                TenantAgnostic = IsMultitenant
            };
            var order = new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
            var page = await store.FindManyAsync(filter, order, pageArgs, cancellationToken);
            var items = page.Where(x => attemptedIds.Add(x.Id)).ToList();

            if (items.Count == 0)
                break;

            leftBehindCount += await ForEachTenantAsync(items, tenants, PurgeBatchAsync, cancellationToken);
        }
    }

    private async Task<int> PurgeBatchAsync(List<BookmarkQueueItem> items, CancellationToken cancellationToken)
    {
        var itemIds = items.Select(x => x.Id).ToList();
        var activeItemIds = (await store.FindManyAsync(new BookmarkQueueFilter
        {
            Ids = itemIds
        }, cancellationToken)).Select(x => x.Id).ToList();
        var deadLetters = (await deadLetterManager.DeadLetterManyAsync(items, "Expired", cancellationToken: cancellationToken)).ToList();
        var deletedCount = activeItemIds.Count == 0
            ? 0
            : await store.DeleteAsync(new BookmarkQueueFilter
            {
                Ids = activeItemIds
            }, cancellationToken);
        var alreadyRemovedItemIds = itemIds.Except(activeItemIds).ToHashSet();

        if (alreadyRemovedItemIds.Count > 0)
        {
            foreach (var deadLetter in deadLetters.Where(x => x.CanReplay && x.Reason == "Expired" && alreadyRemovedItemIds.Contains(x.OriginalQueueItemId)))
            {
                deadLetter.CanReplay = false;
                await deadLetterStore.SaveAsync(deadLetter, cancellationToken);
            }
        }

        logger.LogInformation(
            "Recorded {DeadLetterCount} expired bookmark queue dead-letter items and deleted {DeletedCount} active queue items.",
            deadLetters.Count,
            deletedCount);

        return (int)Math.Max(0, activeItemIds.Count - deletedCount);
    }

    private async Task PurgeDeadLettersAsync(DateTimeOffset now, Dictionary<string, Tenant?> tenants, CancellationToken cancellationToken)
    {
        var thresholdDate = now - options.Value.DeadLetterTtl;
        var attemptedIds = new HashSet<string>();
        var leftBehindCount = 0;

        logger.LogDebug("Purging bookmark queue dead-letter items older than {ThresholdDate}.", thresholdDate);

        while (true)
        {
            var pageArgs = PageArgs.FromRange(leftBehindCount, options.Value.BatchSize);
            var filter = new BookmarkQueueDeadLetterFilter
            {
                DeadLetteredAtLessThan = thresholdDate,
                TenantAgnostic = IsMultitenant
            };
            var order = new BookmarkQueueDeadLetterItemOrder<DateTimeOffset>(x => x.DeadLetteredAt, OrderDirection.Ascending);
            var page = await deadLetterStore.FindManyAsync(filter, order, pageArgs, cancellationToken);
            var items = page.Where(x => attemptedIds.Add(x.Id)).ToList();

            if (items.Count == 0)
                break;

            leftBehindCount += await ForEachTenantAsync(items, tenants, DeleteBatchAsync, cancellationToken);
        }
    }

    private async Task<int> DeleteBatchAsync(List<BookmarkQueueDeadLetterItem> items, CancellationToken cancellationToken)
    {
        var deletedCount = await deadLetterStore.DeleteAsync(new BookmarkQueueDeadLetterFilter
        {
            Ids = items.Select(x => x.Id).ToList()
        }, cancellationToken);

        logger.LogInformation("Purged {Count} bookmark queue dead-letter items.", deletedCount);

        return (int)Math.Max(0, items.Count - deletedCount);
    }

    /// <summary>
    /// Runs <paramref name="purgeAsync"/> once per tenant group of the batch, or once for the whole batch when not multitenant.
    /// </summary>
    /// <returns>The number of items left in the store, so the caller can read past them.</returns>
    private async Task<int> ForEachTenantAsync<T>(List<T> items, Dictionary<string, Tenant?> tenants, Func<List<T>, CancellationToken, Task<int>> purgeAsync, CancellationToken cancellationToken) where T : Entity
    {
        if (!IsMultitenant)
            return await purgeAsync(items, cancellationToken);

        var leftBehindCount = 0;

        foreach (var group in items.GroupBy(x => TenantKey.From(x.TenantId)))
        {
            if (!tenants.TryGetValue(group.Key, out var tenant))
            {
                tenant = await tenantService!.FindAsync(group.Key, cancellationToken)
                         ?? (group.Key == Tenant.DefaultTenantId ? Tenant.Default : new Tenant { Id = group.Key, Name = group.Key });
                tenants[group.Key] = tenant;
            }

            try
            {
                using (tenantAccessor!.PushContext(tenant))
                    leftBehindCount += await purgeAsync(group.ToList(), cancellationToken);
            }
            catch (Exception ex) when (IsIsolatable(ex, cancellationToken))
            {
                logger.LogError(ex, "Failed to purge {Count} bookmark queue item(s) of tenant {TenantId}; they will be retried by a later pass.", group.Count(), group.Key);
                leftBehindCount += group.Count();
            }
        }

        return leftBehindCount;
    }

    private bool IsIsolatable(Exception ex, CancellationToken cancellationToken) =>
        IsMultitenant && !ex.IsFatal() && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested);
}
