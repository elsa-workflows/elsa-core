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
public class MongoActivityExecutionLogStore : IActivityExecutionStore
{
    private readonly MongoDbStore<ActivityExecutionRecord> _mongoDbStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoActivityExecutionLogStore"/> class.
    /// </summary>
    public MongoActivityExecutionLogStore(MongoDbStore<ActivityExecutionRecord> mongoDbStore)
    {
        _mongoDbStore = mongoDbStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken).ToList();
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.CountAsync(queryable => Filter(queryable, filter).OrderBy(x => x.StartedAt), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.DeleteWhereAsync<string>(queryable => Filter(queryable, filter), x => x.Id, cancellationToken);
    }

    private IMongoQueryable<ActivityExecutionRecord> Filter(IMongoQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) =>
        (filter.Apply(queryable) as IMongoQueryable<ActivityExecutionRecord>)!;

    private IMongoQueryable<ActivityExecutionRecord> Order<TOrderBy>(IMongoQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordOrder<TOrderBy> order) =>
        (queryable.OrderBy(order) as IMongoQueryable<ActivityExecutionRecord>)!;

    private IMongoQueryable<ActivityExecutionRecord> Paginate(IMongoQueryable<ActivityExecutionRecord> queryable, PageArgs pageArgs) =>
        (queryable.Paginate(pageArgs) as IMongoQueryable<ActivityExecutionRecord>)!;
}