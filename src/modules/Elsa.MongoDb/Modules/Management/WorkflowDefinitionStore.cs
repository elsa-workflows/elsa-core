using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDb.Common;
using Elsa.MongoDb.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.MongoDb.Modules.Management;

/// <inheritdoc />
[UsedImplicitly]
public class MongoWorkflowDefinitionStore(MongoDbStore<WorkflowDefinition> mongoDbStore) : IWorkflowDefinitionStore
{
    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await mongoDbStore.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
        var results = await mongoDbStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await mongoDbStore.CountAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken);
        var results = await mongoDbStore.FindManyAsync(queryable => OrderAndPaginate(Filter(queryable, filter), order, pageArgs), cancellationToken).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = mongoDbStore.GetCollection();
        var queryable = Filter(collection.AsQueryable(), filter);
        var count = queryable.LongCount();
        queryable = (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;
        var documents = await queryable.Select(ExpressionHelpers.WorkflowDefinitionSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = mongoDbStore.GetCollection();
        var queryable = Order(Filter(collection.AsQueryable(), filter), order);
        var count = queryable.LongCount();
        var mongoQueryable = (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;
        var documents = await mongoQueryable.Select(ExpressionHelpers.WorkflowDefinitionSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(
                query => Filter(query, filter),
                ExpressionHelpers.WorkflowDefinitionSummary,
                cancellationToken)
            .ToList()
            .AsEnumerable();
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(
                query => Order(Filter(query, filter), order),
                ExpressionHelpers.WorkflowDefinitionSummary,
                cancellationToken)
            .ToList()
            .AsEnumerable();
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        return mongoDbStore.FindAsync(queryable => Order(Filter(queryable, filter), order), cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.SaveAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.SaveManyAsync(definitions.Select(i => i), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var queryable = mongoDbStore.GetCollection().AsQueryable();
        var ids = await Filter(queryable, filter).Select(x => x.Id).Distinct().ToListAsync(cancellationToken);
        return await mongoDbStore.DeleteWhereAsync(x => ids.Contains(x.Id), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken).Any();
    }

    /// <inheritdoc />
    public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        return mongoDbStore.CountAsync(queryable => queryable, x => x.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var exists = await mongoDbStore.AnyAsync(x => x.Name == name && x.DefinitionId != definitionId, cancellationToken);
        return !exists;
    }

    private IMongoQueryable<WorkflowDefinition> Filter(IMongoQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter)
    {
        return (filter.Apply(queryable) as IMongoQueryable<WorkflowDefinition>)!;
    }

    private IMongoQueryable<WorkflowDefinition> Order<TOrderBy>(IMongoQueryable<WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order)
    {
        return (queryable.OrderBy(order) as IMongoQueryable<WorkflowDefinition>)!;
    }

    private IMongoQueryable<WorkflowDefinition> Paginate(IMongoQueryable<WorkflowDefinition> queryable, PageArgs pageArgs) =>
        
        (queryable.Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;

    private IMongoQueryable<WorkflowDefinition> OrderAndPaginate<TOrderBy>(IMongoQueryable<WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs)
    {
        return (queryable.OrderBy(order).Paginate(pageArgs) as IMongoQueryable<WorkflowDefinition>)!;
    }
}