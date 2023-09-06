using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDb.Common;
using Elsa.MongoDb.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDb.Modules.Management;

/// <summary>
/// A MongoDb implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class MongoWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly MongoDbStore<WorkflowInstance> _mongoDbStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoWorkflowInstanceStore(MongoDbStore<WorkflowInstance> mongoDbStore)
    {
        _mongoDbStore = mongoDbStore;
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoDbStore.CountAsync(query => Filter(query, filter), cancellationToken);
        var documents = await _mongoDbStore.FindManyAsync(query => Paginate(Filter(query, filter), pageArgs), cancellationToken).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _mongoDbStore.CountAsync(query => Filter(query, filter), cancellationToken);
        var documents = await _mongoDbStore.FindManyAsync(query => OrderAndPaginate(Filter(query, filter), order, pageArgs), cancellationToken).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(query => Order(Filter(query, filter), order), cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoDbStore.CountAsync(query => Filter(query, filter), cancellationToken);
        var documents = await _mongoDbStore.FindManyAsync<WorkflowInstanceSummary>(query => Paginate(Filter(query, filter), pageArgs), ExpressionHelpers.WorkflowInstanceSummary, cancellationToken).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var collection = _mongoDbStore.GetCollection();
        var queryable = Order(Filter(collection.AsQueryable(), filter), order);
        var count = queryable.LongCount();
        var mongoQueryable = (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowInstance>)!;
        var documents = await mongoQueryable.Select(ExpressionHelpers.WorkflowInstanceSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(query => Filter(query, filter), ExpressionHelpers.WorkflowInstanceSummary, cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(query => Order(Filter(query, filter), order), ExpressionHelpers.WorkflowInstanceSummary, cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveManyAsync(instances.Select(i => i), cancellationToken);
    }

    private IMongoQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> queryable, WorkflowInstanceFilter filter)
    {
        return (filter.Apply(queryable.Select(i => i)) as IMongoQueryable<WorkflowInstance>)!;
    }

    private IMongoQueryable<WorkflowInstance> Order<TOrderBy>(IMongoQueryable<WorkflowInstance> queryable, WorkflowInstanceOrder<TOrderBy> order)
    {
        return (queryable.OrderBy(order) as IMongoQueryable<WorkflowInstance>)!;
    }

    private IMongoQueryable<WorkflowInstance> Paginate(IMongoQueryable<WorkflowInstance> queryable, PageArgs pageArgs)
    {
        return (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowInstance>)!;
    }

    private IMongoQueryable<WorkflowInstance> OrderAndPaginate<TOrderBy>(IMongoQueryable<WorkflowInstance> queryable, WorkflowInstanceOrder<TOrderBy> order, PageArgs pageArgs)
    {
        return (queryable.OrderBy(order).Paginate(pageArgs) as IMongoQueryable<WorkflowInstance>)!;
    }
}