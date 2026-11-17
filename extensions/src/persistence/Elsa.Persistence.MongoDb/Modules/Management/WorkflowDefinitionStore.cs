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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
        var tenantAgnostic = filter.TenantAgnostic;
        var count = await mongoDbStore.CountAsync(queryable => Filter(queryable, filter), tenantAgnostic, cancellationToken);
        var documents = await mongoDbStore.FindManyAsync(
                queryable => Paginate(Filter(queryable, filter), pageArgs),
                ExpressionHelpers.WorkflowDefinitionSummary,
                tenantAgnostic,
                cancellationToken)
            .ToList();

        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var tenantAgnostic = filter.TenantAgnostic;
        var count = await mongoDbStore.CountAsync(queryable => Order(Filter(queryable, filter), order), tenantAgnostic, cancellationToken);
        var documents = await mongoDbStore.FindManyAsync(
                queryable => OrderAndPaginate(Filter(queryable, filter), order, pageArgs),
                ExpressionHelpers.WorkflowDefinitionSummary,
                tenantAgnostic,
                cancellationToken)
            .ToList();

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
    /// <remarks>
    /// Creating a new version requires a transaction-capable MongoDB deployment such as a replica set.
    /// Replacing an existing version uses a single conditional document write.
    /// Callbacks execute at most once. Infrastructure and ambiguous commit errors propagate.
    /// </remarks>
    public async Task<WorkflowDefinitionUpdateResult> TryUpdateLatestAsync(
        WorkflowDefinitionFilter filter,
        Func<WorkflowDefinition, bool> matchesExpected,
        Func<WorkflowDefinition, WorkflowDefinition> update,
        CancellationToken cancellationToken = default)
    {
        var candidate = await FindAsync(filter, cancellationToken);
        if (candidate == null)
            return WorkflowDefinitionUpdateResult.NotFound();

        var snapshot = await LoadRawSnapshotAsync(candidate.Id, cancellationToken);
        if (snapshot == null)
            return WorkflowDefinitionUpdateResult.Conflict();

        // Evaluate authorization and construct the update from the same document used in the
        // write predicate, including fields absent in legacy rows.
        var current = BsonSerializer.Deserialize<WorkflowDefinition>(snapshot);
        if (current.DefinitionId != candidate.DefinitionId || current.TenantId != candidate.TenantId ||
            !current.IsLatest || !Filter(new[] { current }.AsQueryable(), filter).Any() || !matchesExpected(current))
        {
            return WorkflowDefinitionUpdateResult.Conflict();
        }

        var next = update(current);
        if (!string.Equals(next.DefinitionId, candidate.DefinitionId, StringComparison.Ordinal))
            throw new InvalidOperationException("An atomic workflow update cannot change its logical definition.");

        next.TenantId = candidate.TenantId;
        var collection = mongoDbStore.GetCollection();
        var snapshotFilter = CreateExactSnapshotFilter(snapshot);

        if (next.Id == candidate.Id)
        {
            // One atomic document replace also works against a standalone server.
            var replace = await collection.ReplaceOneAsync(snapshotFilter, next,
                new ReplaceOptions { Collation = Collation.Simple }, cancellationToken);
            return replace.MatchedCount == 1
                ? WorkflowDefinitionUpdateResult.Updated(next)
                : WorkflowDefinitionUpdateResult.Conflict();
        }

        using var session = await collection.Database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction(new TransactionOptions(
            readConcern: ReadConcern.Snapshot,
            readPreference: ReadPreference.Primary,
            writeConcern: WriteConcern.WMajority));

        try
        {
            var updateDefinition = Builders<WorkflowDefinition>.Update.Set(x => x.IsLatest, false);
            var unmarked = await collection.UpdateOneAsync(session, snapshotFilter, updateDefinition,
                new UpdateOptions { Collation = Collation.Simple }, cancellationToken);
            if (unmarked.MatchedCount != 1)
            {
                await AbortTransactionQuietlyAsync(session);
                return WorkflowDefinitionUpdateResult.Conflict();
            }

            next.IsLatest = true;
            await collection.InsertOneAsync(session, next, cancellationToken: cancellationToken);
        }
        catch (MongoException exception) when (IsKnownAbortedConflict(exception))
        {
            await AbortTransactionQuietlyAsync(session);
            return WorkflowDefinitionUpdateResult.Conflict();
        }
        catch
        {
            await AbortTransactionQuietlyAsync(session);
            throw;
        }

        // An unknown commit result is not proof that the transaction aborted.
        await session.CommitTransactionAsync(cancellationToken);
        return WorkflowDefinitionUpdateResult.Updated(next);
    }

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        try
        {
            await mongoDbStore.AddAsync(definition, cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
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

    private async Task<BsonDocument?> LoadRawSnapshotAsync(string id, CancellationToken cancellationToken)
    {
        var collection = mongoDbStore.GetCollection();
        var rawCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        return await rawCollection
            .Find(Builders<BsonDocument>.Filter.Eq("_id", id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static FilterDefinition<WorkflowDefinition> CreateExactSnapshotFilter(BsonDocument snapshot)
    {
        var filter = new BsonDocument("_id", snapshot["_id"]);
        filter.Add("$expr", new BsonDocument("$eq", new BsonArray
        {
            "$$ROOT",
            new BsonDocument("$literal", snapshot)
        }));

        return new BsonDocumentFilterDefinition<WorkflowDefinition>(filter);
    }

    private static bool IsKnownAbortedConflict(MongoException exception) =>
        exception is MongoCommandException { Code: 112 or 11000 }
        || exception is MongoWriteException { WriteError.Code: 112 or 11000 };

    private static async Task AbortTransactionQuietlyAsync(IClientSessionHandle session)
    {
        if (!session.IsInTransaction)
        {
            return;
        }

        try
        {
            await session.AbortTransactionAsync(CancellationToken.None);
        }
        catch (MongoException)
        {
            // Preserve the original operation error; commit failures are handled outside the transaction body.
        }
    }
}
