using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IWorkflowExecutionLogStore"/>.
/// </summary>
public class EFCoreWorkflowExecutionLogStore(EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> store, ISafeSerializer safeSerializer) : IWorkflowExecutionLogStore
{
    /// <inheritdoc />
    public async Task AddAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => await store.AddAsync(record, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task AddManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await store.AddManyAsync(records, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(record, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await store.SaveManyAsync(records, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(x => x.Timestamp).Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order).Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    private ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity, CancellationToken cancellationToken)
    {
        entity = entity.SanitizeLogMessage();
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = ShouldSerializePayload(entity) ? safeSerializer.Serialize(entity.Payload) : null;
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return;

        entity.Payload = await LoadPayload(dbContext, entity);
    }

    private ValueTask<object?> LoadPayload(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var json = dbContext.Entry(entity).Property<string>("SerializedPayload").CurrentValue;
        return new(!string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<object>(json) : null);
    }

    private bool ShouldSerializePayload(WorkflowExecutionLogRecord source)
    {
        return source.Payload switch
        {
            null => false,
            IDictionary<string, object> dictionary => dictionary.Count > 0,
            _ => true
        };
    }

    private static IQueryable<WorkflowExecutionLogRecord> Filter(IQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordFilter filter) => filter.Apply(queryable);
}