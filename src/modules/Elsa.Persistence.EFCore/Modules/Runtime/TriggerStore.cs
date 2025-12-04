using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <inheritdoc />
[UsedImplicitly]
public class EFCoreTriggerStore(
    EntityStore<RuntimeElsaDbContext, StoredTrigger> store,
    IPayloadSerializer serializer,
    ILogger<EFCoreTriggerStore> logger) : ITriggerStore
{
    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(record, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        await store.SaveManyAsync(records, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(filter.Apply, OnLoadAsync, filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(filter.Apply, OnLoadAsync, filter.TenantAgnostic, cancellationToken);
    }

    public ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return FindManyAsync(filter, pageArgs, new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Ascending), cancellationToken);
    }

    public async ValueTask<Page<StoredTrigger>> FindManyAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(filter.Apply, OnLoadAsync, cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => filter.Apply(queryable).OrderBy(order).Paginate(pageArgs).OrderBy(order), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var removedList = removed.ToList();

        if(removedList.Count > 0)
        {
            var filter = new TriggerFilter { Ids = removedList.Select(r => r.Id).ToList() };
            await DeleteManyAsync(filter, cancellationToken);
        }

        // Retry logic to handle duplicate key violations from race conditions
        const int maxRetries = 3;
        var attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                await store.SaveManyAsync(added, OnSaveAsync, cancellationToken);
                return; // Success
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex) && attempt < maxRetries - 1)
            {
                attempt++;
                logger.LogWarning(ex,
                    "Duplicate key violation detected while saving triggers (attempt {Attempt}/{MaxRetries}). " +
                    "This indicates a race condition in trigger registration. Retrying after removing duplicates.",
                    attempt, maxRetries);

                // Filter out triggers that already exist to avoid duplicate insertion
                var addedList = added.ToList();
                var existingTriggers = await FindManyDuplicatesAsync(addedList, cancellationToken);
                var existingHashes = new HashSet<(string WorkflowDefinitionId, string Hash, string ActivityId)>(
                    existingTriggers.Select(t => (t.WorkflowDefinitionId, t.Hash, t.ActivityId)));

                // Remove duplicates from the list to add
                added = addedList.Where(t => !existingHashes.Contains((t.WorkflowDefinitionId, t.Hash, t.ActivityId)));

                if (!added.Any())
                {
                    logger.LogInformation("All triggers already exist in the database. No duplicates to add.");
                    return; // All triggers already exist, nothing to do
                }
            }
        }
    }

    private async ValueTask<List<StoredTrigger>> FindManyDuplicatesAsync(List<StoredTrigger> triggers, CancellationToken cancellationToken)
    {
        if (!triggers.Any())
            return new List<StoredTrigger>();

        // Query for existing triggers that match the unique key (WorkflowDefinitionId + Hash + ActivityId)
        var workflowDefinitionIds = triggers.Select(t => t.WorkflowDefinitionId).Distinct().ToList();
        var filter = new TriggerFilter { WorkflowDefinitionIds = workflowDefinitionIds };
        var existingTriggers = (await FindManyAsync(filter, cancellationToken)).ToList();

        return existingTriggers;
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        // Check for duplicate key violation error messages across different database providers
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("cannot insert duplicate", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("23505", StringComparison.OrdinalIgnoreCase); // PostgreSQL unique violation code
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, StoredTrigger entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload != null ? serializer.Serialize(entity.Payload) : null;
        return default;
    }

    private ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, StoredTrigger? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return ValueTask.CompletedTask;

        var json = dbContext.Entry(entity).Property<string>("SerializedPayload").CurrentValue;
        entity.Payload = !string.IsNullOrEmpty(json) ? serializer.Deserialize(json) : null;

        return ValueTask.CompletedTask;
    }
}