using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Serialization;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Open.Linq.AsyncExtensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> _store;
    
    /// <summary>
    /// Constructor
    /// </summary>

    public EFCoreWorkflowExecutionLogStore(EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
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
        return await _store.QueryAsync(queryable => Filter(queryable, filter), Load, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), Load, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(x => x.Timestamp), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), Load, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), Load, cancellationToken).ToList();
        return new(results, count);
    }

    private ValueTask<WorkflowExecutionLogRecord> SaveAsync(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity, CancellationToken cancellationToken)
    {
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        dbContext.Entry(entity).Property("ActivityData").CurrentValue = JsonSerializer.Serialize(entity.ActivityState);
        dbContext.Entry(entity).Property("PayloadData").CurrentValue = JsonSerializer.Serialize(entity.Payload, options);
        return new ValueTask<WorkflowExecutionLogRecord>(entity);
    }
    
    private WorkflowExecutionLogRecord? Load(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord? entity)
    {
        if (entity is not null)
        {
            entity.Payload = LoadPayload(dbContext, entity);
            entity.ActivityState = LoadActivityState(dbContext, entity);
        }

        return entity;
    }
    
    private object? LoadPayload(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = dbContext.Entry(entity).Property<string>("PayloadData").CurrentValue;
        return !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<object>(json) : null;
    }
    
    private IDictionary<string, object>? LoadActivityState(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = dbContext.Entry(entity).Property<string>("ActivityData").CurrentValue;
        return !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<IDictionary<string, object>>(json) : null;
    }
    
    private IQueryable<WorkflowExecutionLogRecord> Filter(IQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordFilter filter) => filter.Apply(queryable);

    private IQueryable<WorkflowExecutionLogRecord> Paginate(IQueryable<WorkflowExecutionLogRecord> queryable, PageArgs? pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        return queryable;
    }
}