using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDB.Stores.Runtime;

/// <inheritdoc />
public class MongoWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly MongoStore<Models.WorkflowExecutionLogRecord> _mongoStore;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Constructor
    /// </summary>
    public MongoWorkflowExecutionLogStore(MongoStore<Models.WorkflowExecutionLogRecord> mongoStore, IPayloadSerializer serializer)
    {
        _mongoStore = mongoStore;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        await _mongoStore.SaveAsync(record.MapToDocument(_serializer), cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await _mongoStore.SaveManyAsync(records.Select(i => i.MapToDocument(_serializer)), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken))?.MapFromDocument(_serializer);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken))?.MapFromDocument(_serializer);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Filter(queryable, filter).OrderBy(x => x.Timestamp), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).Select(i => i.MapFromDocument(_serializer)).ToList();
        return new Page<WorkflowExecutionLogRecord>(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).Select(i => i.MapFromDocument(_serializer)).ToList();
        return new Page<WorkflowExecutionLogRecord>(results, count);
    }

    private IMongoQueryable<Models.WorkflowExecutionLogRecord> Filter(IMongoQueryable<Models.WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordFilter filter) => 
        (filter.Apply(queryable.Select(i => i.MapFromDocument(_serializer))).Select(j => j.MapToDocument(_serializer)) as IMongoQueryable<Models.WorkflowExecutionLogRecord>)!;
    
    private IMongoQueryable<Models.WorkflowExecutionLogRecord> Order<TOrderBy>(IMongoQueryable<Models.WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordOrder<TOrderBy> order) => 
        (queryable.Select(i => i.MapFromDocument(_serializer)).OrderBy(order).Select(i => i.MapToDocument(_serializer)) as IMongoQueryable<Models.WorkflowExecutionLogRecord>)!;

    private IMongoQueryable<Models.WorkflowExecutionLogRecord> Paginate(IMongoQueryable<Models.WorkflowExecutionLogRecord> queryable, PageArgs pageArgs) => 
        (queryable.Select(i => i.MapFromDocument(_serializer)).Paginate(pageArgs).Select(i => i.MapToDocument(_serializer)) as IMongoQueryable<Models.WorkflowExecutionLogRecord>)!;
}