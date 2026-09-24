using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.MongoDb.Modules.Management;

/// <inheritdoc />
[UsedImplicitly]
public class MongoWorkflowDefinitionStore(MongoDbStore<WorkflowDefinition> mongoDbStore) : IWorkflowDefinitionStore
{
    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindAsync(queryable => Filter(queryable, filter), filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindAsync(queryable => Order(Filter(queryable, filter), order), filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var tenantAgnostic = filter.TenantAgnostic;
        var count = await mongoDbStore.CountAsync(queryable => Filter(queryable, filter), tenantAgnostic, cancellationToken);
        var results = await mongoDbStore.FindManyAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), tenantAgnostic, cancellationToken).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var tenantAgnostic = filter.TenantAgnostic;
        var count = await mongoDbStore.CountAsync(queryable => Order(Filter(queryable, filter), order), tenantAgnostic, cancellationToken);
        var results = await mongoDbStore.FindManyAsync(queryable => OrderAndPaginate(Filter(queryable, filter), order, pageArgs), tenantAgnostic, cancellationToken).ToList();
        return new Page<WorkflowDefinition>(results, count);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(queryable => Order(Filter(queryable, filter), order), filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = mongoDbStore.GetCollection();
        var queryable = Filter(collection.AsQueryable(), filter);
        var count = queryable.LongCount();
        queryable = queryable.Paginate(pageArgs)!;
        var documents = await queryable.Select(ExpressionHelpers.WorkflowDefinitionSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var collection = mongoDbStore.GetCollection();
        var queryable = Order(Filter(collection.AsQueryable(), filter), order);
        var count = queryable.LongCount();
        var mongoQueryable = queryable.Paginate(pageArgs)!;
        var documents = await mongoQueryable.Select(ExpressionHelpers.WorkflowDefinitionSummary).ToListAsync(cancellationToken);

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(
                query => Filter(query, filter),
                ExpressionHelpers.WorkflowDefinitionSummary,
                filter.TenantAgnostic,
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
                filter.TenantAgnostic,
                cancellationToken)
            .ToList()
            .AsEnumerable();
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        return mongoDbStore.FindAsync(queryable => Order(Filter(queryable, filter), order), filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.SaveAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionUpdateResult> TryUpdateLatestAsync(
        WorkflowDefinitionFilter filter,
        Func<WorkflowDefinition, bool> matchesExpected,
        Func<WorkflowDefinition, WorkflowDefinition> update,
        CancellationToken cancellationToken = default)
    {
        var current = await FindAsync(filter, cancellationToken);

        if (current is null)
            return WorkflowDefinitionUpdateResult.NotFound();

        if (!current.IsLatest || !matchesExpected(current))
            return WorkflowDefinitionUpdateResult.Conflict();

        var expectedId = current.Id;
        var expectedVersion = current.Version;
        var expectedStringData = current.StringData;
        var expectedName = current.Name;
        var expectedDescription = current.Description;
        var expectedTenantId = current.TenantId;
        var expectedDefinitionId = current.DefinitionId;
        var next = update(current);

        if (!string.Equals(next.DefinitionId, expectedDefinitionId, StringComparison.Ordinal))
            throw new InvalidOperationException("An atomic workflow update cannot change its logical definition.");

        next.TenantId = expectedTenantId;

        var snapshotFilter = Builders<WorkflowDefinition>.Filter.And(
            Builders<WorkflowDefinition>.Filter.Eq(x => x.Id, expectedId),
            Builders<WorkflowDefinition>.Filter.Eq(x => x.Version, expectedVersion),
            Builders<WorkflowDefinition>.Filter.Eq(x => x.IsLatest, true),
            Builders<WorkflowDefinition>.Filter.Eq(x => x.StringData, expectedStringData),
            Builders<WorkflowDefinition>.Filter.Eq(x => x.Name, expectedName),
            Builders<WorkflowDefinition>.Filter.Eq(x => x.Description, expectedDescription),
            Builders<WorkflowDefinition>.Filter.Eq(x => x.TenantId, expectedTenantId));

        var collection = mongoDbStore.GetCollection();

        if (next.Id == expectedId)
        {
            var replaceResult = await collection.ReplaceOneAsync(snapshotFilter, next, cancellationToken: cancellationToken);
            return replaceResult.MatchedCount == 1
                ? WorkflowDefinitionUpdateResult.Updated(next)
                : WorkflowDefinitionUpdateResult.Conflict();
        }

        var unmarked = await collection.UpdateOneAsync(
            snapshotFilter,
            Builders<WorkflowDefinition>.Update.Set(x => x.IsLatest, false),
            cancellationToken: cancellationToken);

        if (unmarked.MatchedCount != 1)
            return WorkflowDefinitionUpdateResult.Conflict();

        next.IsLatest = true;
        await collection.InsertOneAsync(next, cancellationToken: cancellationToken);
        return WorkflowDefinitionUpdateResult.Updated(next);
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
        return await mongoDbStore.DeleteWhereAsync(x => ids.Contains(x.Id), filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), filter.TenantAgnostic, cancellationToken).Any();
    }

    /// <inheritdoc />
    public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        return mongoDbStore.CountAsync(queryable => queryable, x => x.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = null, CancellationToken cancellationToken = default)
    {
        var exists = await mongoDbStore.AnyAsync(x => x.Name == name && x.DefinitionId != definitionId, cancellationToken);
        return !exists;
    }

    private IQueryable<WorkflowDefinition> Filter(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter)
    {
        return filter.Apply(queryable)!;
    }

    private IQueryable<WorkflowDefinition> Order<TOrderBy>(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order)
    {
        return queryable.OrderBy(order)!;
    }

    private IQueryable<WorkflowDefinition> Paginate(IQueryable<WorkflowDefinition> queryable, PageArgs pageArgs) =>
        
        queryable.Paginate(pageArgs)!;

    private IQueryable<WorkflowDefinition> OrderAndPaginate<TOrderBy>(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs)
    {
        return queryable.OrderBy(order).Paginate(pageArgs)!;
    }
}