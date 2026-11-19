using Elsa.Common.Multitenancy;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Stores;

/// <inheritdoc />
public class MemoryWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly MemoryStore<WorkflowExecutionLogRecord> _store;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWorkflowExecutionLogStore"/> class.
    /// </summary>
    public MemoryWorkflowExecutionLogStore(MemoryStore<WorkflowExecutionLogRecord> store) : this(store, null)
    {
    }

    /// <summary>
    /// Initializes the store with ambient tenant isolation.
    /// </summary>
    public MemoryWorkflowExecutionLogStore(MemoryStore<WorkflowExecutionLogRecord> store, ITenantAccessor? tenantAccessor)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task AddAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        Write([record], insertOnly: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        Write(records, insertOnly: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        Write([record], insertOnly: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        Write(records, insertOnly: false);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter).OrderByTimestampThenSequence()).FirstOrDefault()?.Copy();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter).OrderBy(order)).FirstOrDefault()?.Copy();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var result = _store.Query(query => Filter(query, filter).OrderByTimestampThenSequence().Paginate(pageArgs)).Select(x => x.Copy()).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var result = _store.Query(query => Filter(query, filter).OrderBy(order).Paginate(pageArgs)).Select(x => x.Copy()).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var ids = _store.Query(query => filter.Apply(MemoryRuntimeRecordOwnership.Owned(query, _tenantAccessor)))
                .Select(record => record.Id).ToList();
            return Task.FromResult(_store.DeleteMany(ids));
        }
    }

    private void Write(IEnumerable<WorkflowExecutionLogRecord> records, bool insertOnly)
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

    private IQueryable<WorkflowExecutionLogRecord> Filter(IQueryable<WorkflowExecutionLogRecord> records, WorkflowExecutionLogRecordFilter filter) =>
        filter.Apply(MemoryRuntimeRecordOwnership.Visible(records, _tenantAccessor));
}
