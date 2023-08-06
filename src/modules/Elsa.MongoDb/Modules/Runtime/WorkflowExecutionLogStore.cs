using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDb.Modules.Runtime;

/// <inheritdoc />
public class MongoWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly MongoDbStore<WorkflowExecutionLogRecord> _mongoDbStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoWorkflowExecutionLogStore"/> class.
    /// </summary>
    public MongoWorkflowExecutionLogStore(MongoDbStore<WorkflowExecutionLogRecord> mongoDbStore)
    {
        _mongoDbStore = mongoDbStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoDbStore.CountAsync(queryable => Filter(queryable, filter).OrderBy(x => x.Timestamp), cancellationToken);
        var results = await _mongoDbStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).ToList();
        return new Page<WorkflowExecutionLogRecord>(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _mongoDbStore.CountAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken);
        var results = await _mongoDbStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).ToList();
        return new Page<WorkflowExecutionLogRecord>(results, count);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.DeleteWhereAsync<string>(queryable => Filter(queryable, filter), x => x.Id, cancellationToken);
    }

    private IMongoQueryable<WorkflowExecutionLogRecord> Filter(IMongoQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordFilter filter) =>
        (filter.Apply(queryable) as IMongoQueryable<WorkflowExecutionLogRecord>)!;

    private IMongoQueryable<WorkflowExecutionLogRecord> Order<TOrderBy>(IMongoQueryable<WorkflowExecutionLogRecord> queryable, WorkflowExecutionLogRecordOrder<TOrderBy> order) =>
        (queryable.OrderBy(order) as IMongoQueryable<WorkflowExecutionLogRecord>)!;

    private IMongoQueryable<WorkflowExecutionLogRecord> Paginate(IMongoQueryable<WorkflowExecutionLogRecord> queryable, PageArgs pageArgs) =>
        (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowExecutionLogRecord>)!;
}