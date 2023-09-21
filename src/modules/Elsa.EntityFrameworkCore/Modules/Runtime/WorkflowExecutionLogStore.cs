using System.Text.Json;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IWorkflowExecutionLogStore"/>.
/// </summary>
public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> _store;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISafeSerializer _safeSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreWorkflowExecutionLogStore"/> class.
    /// </summary>
    public EFCoreWorkflowExecutionLogStore(EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> store, IPayloadSerializer payloadPayloadSerializer, ISafeSerializer safeSerializer)
    {
        _store = store;
        _payloadSerializer = payloadPayloadSerializer;
        _safeSerializer = safeSerializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await _store.SaveManyAsync(records, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(x => x.Timestamp), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs).OrderBy(order), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteWhereAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    private async ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedActivityState").CurrentValue = entity.ActivityState != null ?await _safeSerializer.SerializeAsync(entity.ActivityState, cancellationToken) : default;
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload != null ? await _safeSerializer.SerializeAsync(entity.Payload, cancellationToken) : default;
    }

    private async ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return;

        entity.Payload = await LoadPayload(dbContext, entity);
        entity.ActivityState = await LoadActivityState(dbContext, entity);
    }

    private ValueTask<object?> LoadPayload(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = dbContext.Entry(entity).Property<string>("SerializedPayload").CurrentValue;
        return new(!string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<object>(json) : null);
    }

    private ValueTask<IDictionary<string, object>?> LoadActivityState(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = dbContext.Entry(entity).Property<string>("SerializedActivityState").CurrentValue;
        return new(!string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<IDictionary<string, object>>(json) : null);
    }

    private static IQueryable<WorkflowExecutionLogRecord> Filter(IQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordFilter filter) => filter.Apply(queryable);

    private static IQueryable<WorkflowExecutionLogRecord> Paginate(IQueryable<WorkflowExecutionLogRecord> queryable, PageArgs? pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        return queryable;
    }
}