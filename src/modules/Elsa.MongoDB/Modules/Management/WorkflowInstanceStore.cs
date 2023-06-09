using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.MongoDB.Helpers;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDB.Modules.Management;

/// <summary>
/// An MongoDb implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class MongoWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly MongoStore<WorkflowInstance> _mongoStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoWorkflowInstanceStore(MongoStore<WorkflowInstance> mongoStore)
    {
        _mongoStore = mongoStore;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) => 
        (await _mongoStore.FindManyAsync(query => Filter(query, filter), cancellationToken).FirstOrDefault());

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var documents = await _mongoStore.FindManyAsync(query => Paginate(Filter(query, filter), pageArgs), cancellationToken).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var documents = await _mongoStore.FindManyAsync(query => OrderAndPaginate(Filter(query, filter), order, pageArgs), cancellationToken).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Filter(query, filter), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Order(Filter(query, filter), order), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var documents = await _mongoStore.FindManyAsync<WorkflowInstanceSummary>(query => Paginate(Filter(query, filter), pageArgs), ExpressionHelpers.WorkflowInstanceSummary, cancellationToken).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var collection = _mongoStore.GetCollection();
        var queryable = Order(Filter(collection.AsQueryable(), filter), order);
        var count = queryable.LongCount();
        var mongoQueryable = (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowInstance>)!;
        var documents = await mongoQueryable.Select(ExpressionHelpers.WorkflowInstanceSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Filter(query, filter), ExpressionHelpers.WorkflowInstanceSummary, cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Order(Filter(query, filter), order), ExpressionHelpers.WorkflowInstanceSummary, cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<int> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) =>
        await _mongoStore.SaveAsync(instance, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default) =>
        await _mongoStore.SaveManyAsync(instances.Select(i => i), cancellationToken);

    private IMongoQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> queryable, WorkflowInstanceFilter filter) => 
        (filter.Apply(queryable.Select(i => i)) as IMongoQueryable<WorkflowInstance>)!;
    
    private IMongoQueryable<WorkflowInstance> Order<TOrderBy>(IMongoQueryable<WorkflowInstance> queryable, WorkflowInstanceOrder<TOrderBy> order) => 
        (queryable.OrderBy(order) as IMongoQueryable<WorkflowInstance>)!;
    
    private IMongoQueryable<WorkflowInstance> Paginate(IMongoQueryable<WorkflowInstance> queryable, PageArgs pageArgs) => 
        (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowInstance>)!;
    
    private IMongoQueryable<WorkflowInstance> OrderAndPaginate<TOrderBy>(IMongoQueryable<WorkflowInstance> queryable, WorkflowInstanceOrder<TOrderBy> order, PageArgs pageArgs) => 
        (queryable.OrderBy(order).Paginate(pageArgs) as IMongoQueryable<WorkflowInstance>)!;
}