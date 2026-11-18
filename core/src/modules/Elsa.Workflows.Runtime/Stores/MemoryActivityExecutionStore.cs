using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// Stores activity execution records in memory.
/// </summary>
public class MemoryActivityExecutionStore : IActivityExecutionStore
{
    private readonly MemoryStore<ActivityExecutionRecord> _store;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryActivityExecutionStore"/> class.
    /// </summary>
    public MemoryActivityExecutionStore(MemoryStore<ActivityExecutionRecord> store) : this(store, null)
    {
    }

    /// <summary>
    /// Initializes the store with ambient tenant isolation.
    /// </summary>
    public MemoryActivityExecutionStore(MemoryStore<ActivityExecutionRecord> store, ITenantAccessor? tenantAccessor)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }
    
    /// <inheritdoc />
    public Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        Write([record], insertOnly: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        Write(records, insertOnly: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        Write(records, insertOnly: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault()?.Copy();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter).OrderBy(order)).Select(x => x.Copy()).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).Select(x => x.Copy()).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, order, cancellationToken);
        return entities.Select(ActivityExecutionRecordSummary.FromRecord);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, cancellationToken);
        return entities.Select(ActivityExecutionRecordSummary.FromRecord);
    }

    /// <inheritdoc />
    public Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var ids = _store.Query(query => filter.Apply(MemoryRuntimeRecordOwnership.Owned(query, _tenantAccessor)))
                .Select(record => record.Id).ToList();
            return Task.FromResult(_store.DeleteMany(ids));
        }
    }

    private void Write(IEnumerable<ActivityExecutionRecord> records, bool insertOnly)
    {
        lock (_store.Sync)
        {
            var owned = records.Select(record =>
            {
                var copy = record.Copy();
                copy.TenantId = MemoryRuntimeRecordOwnership.GetWriteTenant(record, _tenantAccessor);
                MemoryRuntimeRecordOwnership.DemandOwner(_store.FindById(record.Id), _tenantAccessor);
                return copy;
            }).ToList();

            if (insertOnly)
                _store.AddMany(owned, record => record.Id);
            else
                _store.SaveMany(owned, record => record.Id);
        }
    }

    private IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> records, ActivityExecutionRecordFilter filter) =>
        filter.Apply(MemoryRuntimeRecordOwnership.Visible(records, _tenantAccessor));
}
