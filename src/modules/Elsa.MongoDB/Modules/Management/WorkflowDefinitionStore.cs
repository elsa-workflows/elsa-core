using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDB.Modules.Management;

/// <inheritdoc />
public class MongoWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly MongoStore<WorkflowDefinition> _mongoStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoWorkflowDefinitionStore(
        MongoStore<WorkflowDefinition> mongoStore,
        IWorkflowInstanceStore workflowInstanceStore)
    {
        _mongoStore = mongoStore;
        _workflowInstanceStore = workflowInstanceStore;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken));
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => OrderAndPaginate(Filter(queryable, filter), order, pageArgs), cancellationToken).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) => 
        await _mongoStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken);

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = _mongoStore.GetCollection();
        var queryable = Filter(collection.AsQueryable(), filter);
        var count = queryable.LongCount();
        queryable = (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;
        var documents = await queryable.Select(ExpressionHelpers.WorkflowDefinitionSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = _mongoStore.GetCollection();
        var queryable = Order(Filter(collection.AsQueryable(), filter), order);
        var count = queryable.LongCount();
        var mongoQueryable = (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;
        var documents = await mongoQueryable.Select(ExpressionHelpers.WorkflowDefinitionSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Filter(query, filter), ExpressionHelpers.WorkflowDefinitionSummary, cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Order(Filter(query, filter), order), ExpressionHelpers.WorkflowDefinitionSummary, cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        return (await _mongoStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken));
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveAsync(definition, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveManyAsync(definitions.Select(i => i), cancellationToken);

    /// <inheritdoc />
    public async Task<int> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var queryable = _mongoStore.GetCollection().AsQueryable();
        var ids = await Filter(queryable, filter).Select(x => x.Id).Distinct().ToListAsync(cancellationToken);
        await _workflowInstanceStore.DeleteAsync(new WorkflowInstanceFilter { DefinitionVersionIds = ids }, cancellationToken);
        return await _mongoStore.DeleteWhereAsync(x => ids.Contains(x.Id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken).Any();

    private IMongoQueryable<WorkflowDefinition> Filter(IMongoQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<WorkflowDefinition>)!;

    private IMongoQueryable<WorkflowDefinition> Order<TOrderBy>(IMongoQueryable<WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order) => 
        (queryable.OrderBy(order) as IMongoQueryable<WorkflowDefinition>)!;
    
    private IMongoQueryable<WorkflowDefinition> Paginate(IMongoQueryable<WorkflowDefinition> queryable, PageArgs pageArgs) => 
        (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;
    
    private IMongoQueryable<WorkflowDefinition> OrderAndPaginate<TOrderBy>(IMongoQueryable<WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs) => 
        (queryable.OrderBy(order).Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;
}
