using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDB.Modules.Runtime;

/// <inheritdoc />
public class MongoWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly MongoStore<WorkflowExecutionLogRecord> _mongoStore;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Constructor
    /// </summary>
    public MongoWorkflowExecutionLogStore(MongoStore<WorkflowExecutionLogRecord> mongoStore, IPayloadSerializer serializer)
    {
        _mongoStore = mongoStore;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        await _mongoStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await _mongoStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken));
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Filter(queryable, filter).OrderBy(x => x.Timestamp), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).ToList();
        return new Page<WorkflowExecutionLogRecord>(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).ToList();
        return new Page<WorkflowExecutionLogRecord>(results, count);
    }

    private IMongoQueryable<WorkflowExecutionLogRecord> Filter(IMongoQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<WorkflowExecutionLogRecord>)!;
    
    private IMongoQueryable<WorkflowExecutionLogRecord> Order<TOrderBy>(IMongoQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordOrder<TOrderBy> order) => 
        (queryable.OrderBy(order) as IMongoQueryable<WorkflowExecutionLogRecord>)!;

    private IMongoQueryable<WorkflowExecutionLogRecord> Paginate(IMongoQueryable<WorkflowExecutionLogRecord> queryable, PageArgs pageArgs) => 
        (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowExecutionLogRecord>)!;
}