using Elsa.EntityFrameworkCore.Common;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> _store;
    private readonly IWorkflowExecutionLogStateSerializer _serializer;

    /// <summary>
    /// Constructor
    /// </summary>
    public EFCoreWorkflowExecutionLogStore(EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> store, IWorkflowExecutionLogStateSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await _store.SaveManyAsync(records, SaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter), LoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), LoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(x => x.Timestamp), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), LoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), LoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    private async ValueTask<WorkflowExecutionLogRecord> SaveAsync(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("ActivityData").CurrentValue = entity.ActivityState != null ? await _serializer.SerializeAsync(entity.ActivityState, cancellationToken) : default;
        dbContext.Entry(entity).Property("PayloadData").CurrentValue = entity.Payload != null ? await _serializer.SerializeAsync(entity.Payload, cancellationToken) : default;
        return entity;
    }

    private async ValueTask<WorkflowExecutionLogRecord?> LoadAsync(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return entity;

        entity.Payload = await LoadPayload(dbContext, entity, cancellationToken);
        entity.ActivityState = await LoadActivityState(dbContext, entity, cancellationToken);

        return entity;
    }

    private async ValueTask<object?> LoadPayload(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity, CancellationToken cancellationToken)
    {
        var json = dbContext.Entry(entity).Property<string>("PayloadData").CurrentValue;
        return !string.IsNullOrEmpty(json) ? await _serializer.DeserializeAsync(json, cancellationToken) : null;
    }

    private async ValueTask<IDictionary<string, object>?> LoadActivityState(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity, CancellationToken cancellationToken)
    {
        var json = dbContext.Entry(entity).Property<string>("ActivityData").CurrentValue;
        return !string.IsNullOrEmpty(json) ? await _serializer.DeserializeDictionaryAsync(json, cancellationToken) : null;
    }

    private IQueryable<WorkflowExecutionLogRecord> Filter(IQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordFilter filter) => filter.Apply(queryable);

    private IQueryable<WorkflowExecutionLogRecord> Paginate(IQueryable<WorkflowExecutionLogRecord> queryable, PageArgs? pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        return queryable;
    }
}