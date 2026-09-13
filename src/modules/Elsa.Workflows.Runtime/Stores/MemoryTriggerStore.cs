using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Stores;

/// <inheritdoc />
[UsedImplicitly]
public class MemoryTriggerStore : ITriggerStore
{
    private readonly MemoryStore<StoredTrigger> _store;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryTriggerStore"/> class.
    /// </summary>
    public MemoryTriggerStore(MemoryStore<StoredTrigger> store, ITenantAccessor? tenantAccessor = null)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            ApplyCurrentTenant(record);
            EnsureLogicalKeyAvailable(record);
            _store.Save(record, x => x.Id);
        }

        return new();
    }

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var recordList = records.ToList();

            foreach (var record in recordList)
                ApplyCurrentTenant(record);

            var uniqueRecords = DistinctByLogicalKey(recordList).ToList();

            foreach (var record in uniqueRecords)
                EnsureLogicalKeyAvailable(record);

            _store.SaveMany(uniqueRecords, x => x.Id);
        }

        return new();
    }

    /// <inheritdoc />
    public ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(filter.Apply).SingleOrDefault();
        return new(entity);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(filter.Apply);
        return new(entities);
    }

    public ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return FindManyAsync(filter, pageArgs, new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Ascending), cancellationToken);
    }

    public ValueTask<Page<StoredTrigger>> FindManyAsync<TOrderBy>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(filter.Apply).LongCount();
        var result = _store.Query(query => filter.Apply(query).OrderBy(order).Paginate(pageArgs)).ToList();
        return ValueTask.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var removedList = removed.ToList();
            var addedList = added.ToList();

            foreach (var trigger in addedList)
                ApplyCurrentTenant(trigger);

            addedList = DistinctByLogicalKey(addedList).ToList();

            if (removedList.Count > 0)
                _store.DeleteMany(removedList, x => x.Id);

            if (addedList.Count == 0)
                return new();

            var newTriggers = GetMissingLogicalTriggers(addedList);

            if (newTriggers.Count == 0)
                return new();

            _store.SaveMany(newTriggers, x => x.Id);
        }

        return new();
    }

    /// <inheritdoc />
    public ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var ids = _store.Query(filter.Apply).Select(x => x.Id).ToList();
            return new(_store.DeleteMany(ids));
        }
    }

    private void EnsureLogicalKeyAvailable(StoredTrigger record)
    {
        var logicalKey = GetLogicalKey(record);
        var existing = _store.Find(x => x.Id != record.Id && GetLogicalKey(x) == logicalKey);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"A stored trigger already exists for workflow '{record.WorkflowDefinitionId}', hash '{record.Hash}', activity '{record.ActivityId}', tenant '{record.TenantId}'.");
        }
    }

    private List<StoredTrigger> GetMissingLogicalTriggers(ICollection<StoredTrigger> triggers)
    {
        var existingKeys = GetExistingLogicalKeys(triggers);
        return triggers
            .Where(trigger => !existingKeys.Contains(GetLogicalKey(trigger)))
            .ToList();
    }

    private HashSet<string> GetExistingLogicalKeys(ICollection<StoredTrigger> triggers)
    {
        var workflowDefinitionIds = triggers.Select(x => x.WorkflowDefinitionId).Distinct().ToHashSet();
        return _store
            .FindMany(trigger => workflowDefinitionIds.Contains(trigger.WorkflowDefinitionId))
            .Select(GetLogicalKey)
            .ToHashSet(StringComparer.Ordinal);
    }

    private void ApplyCurrentTenant(StoredTrigger trigger)
    {
        if (trigger.TenantId == Tenant.AgnosticTenantId || _tenantAccessor is null)
            return;

        trigger.TenantId ??= _tenantAccessor.TenantId;
    }

    private static IEnumerable<StoredTrigger> DistinctByLogicalKey(IEnumerable<StoredTrigger> triggers)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var trigger in triggers)
        {
            if (seen.Add(GetLogicalKey(trigger)))
                yield return trigger;
        }
    }

    private static string GetLogicalKey(StoredTrigger trigger) =>
        string.Join(
            '\u001f',
            trigger.WorkflowDefinitionId,
            trigger.Hash,
            trigger.ActivityId,
            trigger.TenantId);
}
