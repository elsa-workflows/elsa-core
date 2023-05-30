using Elsa.Common.Entities;
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

/// <inheritdoc />
public class MongoWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly MongoStore<Models.WorkflowDefinition> _mongoStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IPayloadSerializer _payloadSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoWorkflowDefinitionStore(
        MongoStore<Models.WorkflowDefinition> mongoStore,
        IWorkflowInstanceStore workflowInstanceStore,
        IActivitySerializer activitySerializer,
        IPayloadSerializer payloadSerializer)
    {
        _mongoStore = mongoStore;
        _workflowInstanceStore = workflowInstanceStore;
        _activitySerializer = activitySerializer;
        _payloadSerializer = payloadSerializer;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken))?.MapFromDocument(_payloadSerializer);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return (await _mongoStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken))?.MapFromDocument(_payloadSerializer);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).Select(i => i.MapFromDocument(_payloadSerializer)).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _mongoStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken).LongCount();
        var results = await _mongoStore.FindManyAsync(queryable => OrderAndPaginate(Filter(queryable, filter), order, pageArgs), cancellationToken).Select(i => i.MapFromDocument(_payloadSerializer)).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken).Select(i => i.MapFromDocument(_payloadSerializer));

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) => 
        await _mongoStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken).Select(i => i.MapFromDocument(_payloadSerializer));

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = _mongoStore.GetCollection();
        var queryable = Filter(collection.AsQueryable(), filter);
        var count = queryable.LongCount();
        queryable = (queryable.Paginate(pageArgs) as IMongoQueryable<Models.WorkflowDefinition>)!;
        var documents = await queryable.Select(x => WorkflowDefinitionSummary.FromDefinition(x.MapFromDocument(_payloadSerializer))).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = _mongoStore.GetCollection();
        var queryable = Order(Filter(collection.AsQueryable(), filter), order);
        var count = queryable.LongCount();
        var mongoQueryable = (queryable.Paginate(pageArgs) as IMongoQueryable<Models.WorkflowDefinition>)!;
        var documents = await mongoQueryable.Select(x => WorkflowDefinitionSummary.FromDefinition(x.MapFromDocument(_payloadSerializer))).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Filter(query, filter), x => WorkflowDefinitionSummary.FromDefinition(x.MapFromDocument(_payloadSerializer)), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(query => Order(Filter(query, filter), order), x => WorkflowDefinitionSummary.FromDefinition(x.MapFromDocument(_payloadSerializer)), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        return (await _mongoStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken))?.MapFromDocument(_payloadSerializer);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveAsync(definition.MapToDocument(_payloadSerializer), cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveManyAsync(definitions.Select(i => i.MapToDocument(_payloadSerializer)), cancellationToken);

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

    private IMongoQueryable<Models.WorkflowDefinition> Filter(IMongoQueryable<Models.WorkflowDefinition> queryable, WorkflowDefinitionFilter filter) => 
        (filter.Apply(queryable.Select(i => i.MapFromDocument(_payloadSerializer))).Select(j => j.MapToDocument(_payloadSerializer)) as IMongoQueryable<Models.WorkflowDefinition>)!;

    private IMongoQueryable<Models.WorkflowDefinition> Order<TOrderBy>(IMongoQueryable<Models.WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order) => 
        (queryable.Select(i => i.MapFromDocument(_payloadSerializer)).OrderBy(order).Select(i => i.MapToDocument(_payloadSerializer)) as IMongoQueryable<Models.WorkflowDefinition>)!;
    
    private IMongoQueryable<Models.WorkflowDefinition> Paginate(IMongoQueryable<Models.WorkflowDefinition> queryable, PageArgs pageArgs) => 
        (queryable.Select(i => i.MapFromDocument(_payloadSerializer)).Paginate(pageArgs).Select(i => i.MapToDocument(_payloadSerializer)) as IMongoQueryable<Models.WorkflowDefinition>)!;
    
    private IMongoQueryable<Models.WorkflowDefinition> OrderAndPaginate<TOrderBy>(IMongoQueryable<Models.WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs) => 
        (queryable.Select(i => i.MapFromDocument(_payloadSerializer)).OrderBy(order).Paginate(pageArgs).Select(i => i.MapToDocument(_payloadSerializer)) as IMongoQueryable<Models.WorkflowDefinition>)!;
}
