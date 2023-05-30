using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDB.Stores.Management;

/// <summary>
/// An MongoDb implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class MongoWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly MongoStore<Models.WorkflowInstance> _mongoStore;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoWorkflowInstanceStore(MongoStore<Models.WorkflowInstance> mongoStore, IWorkflowStateSerializer workflowStateSerializer)
    {
        _mongoStore = mongoStore;
        _workflowStateSerializer = workflowStateSerializer;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) => 
        (await _mongoStore.FindManyAsync(query => Filter(query, filter), cancellationToken).FirstOrDefault()).MapFromDocument(_workflowStateSerializer);

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var documents = await _mongoStore.FindManyAsync(query => Paginate(Filter(query, filter), pageArgs), cancellationToken).Select(i => i.MapFromDocument(_workflowStateSerializer)).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var documents = await _mongoStore.FindManyAsync(query => OrderAndPaginate(Filter(query, filter), order, pageArgs), cancellationToken).Select(i => i.MapFromDocument(_workflowStateSerializer)).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Filter(query, filter), cancellationToken).Select(i => i.MapFromDocument(_workflowStateSerializer)).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Order(Filter(query, filter), order), cancellationToken).Select(i => i.MapFromDocument(_workflowStateSerializer)).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var documents = await _mongoStore.FindManyAsync<WorkflowInstanceSummary>(query => Paginate(Filter(query, filter), pageArgs), x => WorkflowInstanceSummary.FromInstance(x.MapFromDocument(_workflowStateSerializer)), cancellationToken).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var collection = _mongoStore.GetCollection();
        var queryable = Order(Filter(collection.AsQueryable(), filter), order);
        var count = queryable.LongCount();
        var mongoQueryable = (queryable.Paginate(pageArgs) as IMongoQueryable<Models.WorkflowInstance>)!;
        var documents = await mongoQueryable.Select(x => WorkflowInstanceSummary.FromInstance(x.MapFromDocument(_workflowStateSerializer))).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Filter(query, filter), x => WorkflowInstanceSummary.FromInstance(x.MapFromDocument(_workflowStateSerializer)), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Order(Filter(query, filter), order), x => WorkflowInstanceSummary.FromInstance(x.MapFromDocument(_workflowStateSerializer)), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<int> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) =>
        await _mongoStore.SaveAsync(instance.MapToDocument(_workflowStateSerializer), cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default) =>
        await _mongoStore.SaveManyAsync(instances.Select(i => i.MapToDocument(_workflowStateSerializer)), cancellationToken);

    private IMongoQueryable<Models.WorkflowInstance> Filter(IQueryable<Models.WorkflowInstance> queryable, WorkflowInstanceFilter filter) => 
        (filter.Apply(queryable.Select(i => i.MapFromDocument(_workflowStateSerializer))).Select(j => j.MapToDocument(_workflowStateSerializer)) as IMongoQueryable<Models.WorkflowInstance>)!;
    
    private IMongoQueryable<Models.WorkflowInstance> Order<TOrderBy>(IMongoQueryable<Models.WorkflowInstance> queryable, WorkflowInstanceOrder<TOrderBy> order) => 
        (queryable.Select(i => i.MapFromDocument(_workflowStateSerializer)).OrderBy(order).Select(i => i.MapToDocument(_workflowStateSerializer)) as IMongoQueryable<Models.WorkflowInstance>)!;
    
    private IMongoQueryable<Models.WorkflowInstance> Paginate(IMongoQueryable<Models.WorkflowInstance> queryable, PageArgs pageArgs) => 
        (queryable.Select(i => i.MapFromDocument(_workflowStateSerializer)).Paginate(pageArgs).Select(i => i.MapToDocument(_workflowStateSerializer)) as IMongoQueryable<Models.WorkflowInstance>)!;
    
    private IMongoQueryable<Models.WorkflowInstance> OrderAndPaginate<TOrderBy>(IMongoQueryable<Models.WorkflowInstance> queryable, WorkflowInstanceOrder<TOrderBy> order, PageArgs pageArgs) => 
        (queryable.Select(i => i.MapFromDocument(_workflowStateSerializer)).OrderBy(order).Paginate(pageArgs).Select(i => i.MapToDocument(_workflowStateSerializer)) as IMongoQueryable<Models.WorkflowInstance>)!;
}