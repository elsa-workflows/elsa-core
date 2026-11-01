using Elsa.Persistence.MongoDb.Helpers;
using Elsa.Persistence.MongoDb.Modules.Identity;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Modules.Management;

/// <summary>
/// Names of the unique workflow definition indexes.
/// </summary>
internal static class WorkflowDefinitionIndexes
{
    /// <summary>
    /// The store-wide unique index on (DefinitionId, Version) that earlier versions created.
    /// </summary>
    public const string LegacyDefinitionIdVersionUnique = "DefinitionId_1_Version_1";

    /// <summary>
    /// The per-tenant unique index on (DefinitionId, TenantId, Version) that replaces <see cref="LegacyDefinitionIdVersionUnique"/>.
    /// </summary>
    public const string DefinitionIdTenantIdVersionUnique = "DefinitionId_1_TenantId_1_Version_1";
}

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    private const string DefinitionIdField = nameof(WorkflowDefinition.DefinitionId);
    private const string TenantIdField = nameof(WorkflowDefinition.TenantId);
    private const string VersionField = nameof(WorkflowDefinition.Version);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        return Task.WhenAll(
            CreateWorkflowDefinitionIndices(scope, cancellationToken),
            CreateWorkflowInstanceIndices(scope, cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateWorkflowDefinitionIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowDefinitionCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<WorkflowDefinition>>();
        if (workflowDefinitionCollection == null) return Task.CompletedTask;

        var logger = serviceScope.ServiceProvider.GetService<ILogger<CreateIndices>>() ?? NullLogger<CreateIndices>.Instance;

        return IndexHelpers.CreateAsync(
            workflowDefinitionCollection,
            async (collection, indexBuilder) =>
            {
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowDefinition>>
                    {
                        new(indexBuilder.Ascending(x => x.Version)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.IsSystem)),
                        new(indexBuilder.Ascending(x => x.IsLatest)),
                        new(indexBuilder.Ascending(x => x.IsPublished)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken);
                await EnsureDefinitionIdIndexesAsync(collection, indexBuilder, logger, cancellationToken);
            });
    }

    /// <summary>
    /// Makes workflow definition ids unique per tenant rather than across the store (elsa-core#9162).
    /// </summary>
    /// <remarks>
    /// The compound index is created before the legacy store-wide <c>DefinitionId_1_Version_1</c> index is dropped, so
    /// the collection is never without uniqueness, and existing data always fits it because the legacy index was
    /// stricter. A default-tenant document stores a null TenantId, which MongoDB indexes as null, so two of them with
    /// the same (DefinitionId, Version) still collide. When several nodes start together, creating the same index again
    /// is a no-op, and a node that finds the legacy index already dropped (IndexNotFound) carries on.
    /// </remarks>
    private static async Task EnsureDefinitionIdIndexesAsync(IMongoCollection<WorkflowDefinition> collection, IndexKeysDefinitionBuilder<WorkflowDefinition> indexBuilder, ILogger logger, CancellationToken cancellationToken)
    {
        var existingIndexes = await ListIndexesAsync(collection, cancellationToken);
        var storeWideIndexes = existingIndexes.Where(x => HasKeys(x, ascendingOnly: false, DefinitionIdField, VersionField) && IsPlainUnique(x)).ToList();
        var tenantIndex = existingIndexes.FirstOrDefault(x => HasKeys(x, ascendingOnly: false, DefinitionIdField, TenantIdField, VersionField) && IsPlainUnique(x));

        if (tenantIndex != null)
        {
            logger.LogDebug("Workflow definition unique index '{IndexName}' on (DefinitionId, TenantId, Version) is already present.", GetIndexName(tenantIndex));
        }
        else if (existingIndexes.FirstOrDefault(x => HasKeys(x, ascendingOnly: true, DefinitionIdField, TenantIdField, VersionField)) is { } weakIndex)
        {
            // MongoDB refuses a second index with the same keys. Keep the store-wide index if it still protects
            // uniqueness, otherwise refuse to start rather than let a tenant save duplicate definition versions.
            var weakIndexName = GetIndexName(weakIndex);

            if (storeWideIndexes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The workflow definition index '{weakIndexName}' on (DefinitionId, TenantId, Version) is not a plain unique index, and no other index keeps definition versions unique. " +
                    $"Drop '{weakIndexName}' (and remove any duplicate definition versions within a tenant), then restart so the unique index '{WorkflowDefinitionIndexes.DefinitionIdTenantIdVersionUnique}' can be created.");
            }

            logger.LogWarning(
                "The workflow definition index '{IndexName}' on (DefinitionId, TenantId, Version) is not a plain unique index, so the store-wide unique index '{LegacyIndexName}' is kept and a definition cannot exist in two tenants. Drop '{IndexName}' and restart to make definition ids unique per tenant.",
                weakIndexName,
                GetIndexName(storeWideIndexes[0]),
                weakIndexName);
            return;
        }
        else
        {
            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<WorkflowDefinition>(
                    indexBuilder.Ascending(x => x.DefinitionId).Ascending(x => x.TenantId).Ascending(x => x.Version),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = WorkflowDefinitionIndexes.DefinitionIdTenantIdVersionUnique
                    }),
                cancellationToken: cancellationToken);
            logger.LogDebug("Created workflow definition unique index '{IndexName}' on (DefinitionId, TenantId, Version).", WorkflowDefinitionIndexes.DefinitionIdTenantIdVersionUnique);
        }

        await DropLegacyIndexAsync(collection, existingIndexes, logger, cancellationToken);

        // Only DefinitionId_1_Version_1 is dropped automatically. A store-wide unique index under another name was
        // created by someone else, so it is left alone, but it keeps definition ids unique across tenants.
        foreach (var index in storeWideIndexes.Where(x => GetIndexName(x) != WorkflowDefinitionIndexes.LegacyDefinitionIdVersionUnique))
        {
            logger.LogWarning(
                "The workflow definition index '{IndexName}' keeps definition versions unique across the whole store, so tenants still cannot share a definition. Drop '{IndexName}' to make definition ids unique per tenant through '{TenantIndexName}'.",
                GetIndexName(index),
                GetIndexName(index),
                WorkflowDefinitionIndexes.DefinitionIdTenantIdVersionUnique);
        }
    }

    private static async Task DropLegacyIndexAsync(IMongoCollection<WorkflowDefinition> collection, IEnumerable<BsonDocument> existingIndexes, ILogger logger, CancellationToken cancellationToken)
    {
        if (!existingIndexes.Any(x => GetIndexName(x) == WorkflowDefinitionIndexes.LegacyDefinitionIdVersionUnique))
        {
            logger.LogDebug("Workflow definition unique index '{IndexName}' was not found.", WorkflowDefinitionIndexes.LegacyDefinitionIdVersionUnique);
            return;
        }

        try
        {
            await collection.Indexes.DropOneAsync(WorkflowDefinitionIndexes.LegacyDefinitionIdVersionUnique, cancellationToken);
            logger.LogInformation("Dropped the store-wide workflow definition unique index '{IndexName}'; definition versions are now unique per tenant through '{TenantIndexName}'.", WorkflowDefinitionIndexes.LegacyDefinitionIdVersionUnique, WorkflowDefinitionIndexes.DefinitionIdTenantIdVersionUnique);
        }
        catch (MongoCommandException exception) when (IsIndexNotFound(exception))
        {
            logger.LogDebug("Workflow definition unique index '{IndexName}' was already dropped.", WorkflowDefinitionIndexes.LegacyDefinitionIdVersionUnique);
        }
    }

    private static bool IsIndexNotFound(MongoCommandException exception) =>
        exception.Code == IdentityRoleIndexes.IndexNotFoundCode
        || string.Equals(exception.CodeName, "IndexNotFound", StringComparison.Ordinal)
        || exception.InnerException is MongoCommandException inner && IsIndexNotFound(inner);

    private static async Task<List<BsonDocument>> ListIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken)
    {
        using var cursor = await collection.Indexes.ListAsync(cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    private static string? GetIndexName(BsonDocument index) =>
        index.TryGetValue("name", out var name) && name.BsonType == BsonType.String ? name.AsString : null;

    /// <summary>
    /// Whether the index keys are exactly the given fields in order: ascending only, or in any direction.
    /// </summary>
    private static bool HasKeys(BsonDocument index, bool ascendingOnly, params string[] fields)
    {
        if (!index.TryGetValue("key", out var key) || key is not BsonDocument keyDocument || keyDocument.ElementCount != fields.Length)
        {
            return false;
        }

        return fields.Select((field, position) => (field, element: keyDocument.GetElement(position))).All(x =>
            string.Equals(x.element.Name, x.field, StringComparison.Ordinal)
            && x.element.Value.IsNumeric
            && (ascendingOnly ? x.element.Value.ToDouble() > 0 : x.element.Value.ToDouble() != 0));
    }

    /// <summary>
    /// Whether the index is unique over every document, rather than non-unique or limited by a partial filter.
    /// </summary>
    private static bool IsPlainUnique(BsonDocument index) =>
        index.TryGetValue("unique", out var unique) && unique.ToBoolean() && !index.Contains("partialFilterExpression");

    private static Task CreateWorkflowInstanceIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowInstanceCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<WorkflowInstance>>();
        if (workflowInstanceCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowInstanceCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowInstance>>
                    {
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
                            .Ascending(x => x.Version)
                            .Ascending(x => x.SubStatus)
                            .Ascending(x => x.Status)),
                        new(indexBuilder
                            .Ascending(x => x.SubStatus)
                            .Ascending(x => x.Status)),
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
                            .Ascending(x => x.Status)),
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
                            .Ascending(x => x.SubStatus)),
                        new(indexBuilder.Ascending(x => x.IsExecuting)),
                        new(indexBuilder.Ascending(x => x.DefinitionId)),
                        new(indexBuilder.Ascending(x => x.Status)),
                        new(indexBuilder.Ascending(x => x.SubStatus)),
                        new(indexBuilder.Ascending(x => x.CorrelationId)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.IsSystem)),
                        new(indexBuilder.Ascending(x => x.CreatedAt)),
                        new(indexBuilder.Ascending(x => x.UpdatedAt)),
                        new(indexBuilder.Ascending(x => x.FinishedAt)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }
}

