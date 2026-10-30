using Elsa.Identity.Entities;
using Elsa.Persistence.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Modules.Identity;

/// <summary>
/// Names of the unique role name indexes.
/// </summary>
internal static class IdentityRoleIndexes
{
    /// <summary>
    /// The store-wide unique index on <see cref="Role.Name"/> that earlier versions created.
    /// </summary>
    public const string LegacyNameUnique = "Name_1";

    /// <summary>
    /// The per-tenant unique index on (TenantId, Name) that replaces <see cref="LegacyNameUnique"/>.
    /// </summary>
    public const string TenantIdNameUnique = "TenantId_1_Name_1";

    /// <summary>
    /// The server error code for a missing index (<c>IndexNotFound</c>).
    /// </summary>
    public const int IndexNotFoundCode = 27;
}

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        await Task.WhenAll(
            CreateApplicationIndices(scope, cancellationToken),
            CreateUserIndices(scope, cancellationToken),
            CreateRoleIndices(scope, cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateApplicationIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var applicationCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<Application>>();
        if (applicationCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            applicationCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<Application>>
                    {
                        new(indexBuilder.Ascending(x => x.ClientId), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.Name), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }

    private static Task CreateUserIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var userCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<User>>();
        if (userCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            userCollection,
            async (collection, indexBuilder) =>
            {
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<User>>
                    {
                        new(indexBuilder.Ascending(x => x.Name),
                            new CreateIndexOptions
                            {
                                Unique = true
                            }),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken);
            });
    }

    private static Task CreateRoleIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var roleCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<Role>>();
        if (roleCollection == null) return Task.CompletedTask;

        var logger = serviceScope.ServiceProvider.GetService<ILogger<CreateIndices>>() ?? NullLogger<CreateIndices>.Instance;

        return IndexHelpers.CreateAsync(
            roleCollection,
            (collection, indexBuilder) => EnsureRoleNameIndexesAsync(collection, indexBuilder, logger, cancellationToken));
    }

    /// <summary>
    /// What the existing indexes say about per-tenant role name uniqueness.
    /// </summary>
    private enum TenantRoleNameIndexState
    {
        /// <summary>No index on (TenantId, Name) exists yet, so the unique one can be created.</summary>
        Missing,

        /// <summary>A plain unique index on (TenantId, Name) exists, in any key direction and under any name.</summary>
        Unique,

        /// <summary>
        /// An ascending (TenantId, Name) index exists that is not plain unique. MongoDB refuses a second index with the
        /// same keys, so the unique one cannot be created until the operator drops it.
        /// </summary>
        Weak
    }

    /// <summary>
    /// Makes role names unique per tenant rather than across the store (elsa-core#8615).
    /// </summary>
    /// <remarks>
    /// The compound index is created before the legacy store-wide <c>Name_1</c> index is dropped, so the collection is
    /// never without name uniqueness, and existing data always fits it because <c>Name_1</c> was stricter. When several
    /// nodes start together, creating the same index again is a no-op, and a node that finds <c>Name_1</c> already
    /// dropped (IndexNotFound) carries on instead of failing to start.
    /// </remarks>
    private static async Task EnsureRoleNameIndexesAsync(IMongoCollection<Role> collection, IndexKeysDefinitionBuilder<Role> indexBuilder, ILogger logger, CancellationToken cancellationToken)
    {
        var existingIndexes = await ListIndexesAsync(collection, cancellationToken);
        var (state, stateIndex) = GetTenantRoleNameIndexState(existingIndexes);

        switch (state)
        {
            case TenantRoleNameIndexState.Unique:
                logger.LogDebug("Role unique index '{IndexName}' on (TenantId, Name) is already present.", GetIndexName(stateIndex!));
                break;

            case TenantRoleNameIndexState.Missing:
                await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<Role>(
                        indexBuilder.Ascending(x => x.TenantId).Ascending(x => x.Name),
                        new CreateIndexOptions
                        {
                            Unique = true,
                            Name = IdentityRoleIndexes.TenantIdNameUnique
                        }),
                    cancellationToken: cancellationToken);
                logger.LogDebug("Created role unique index '{IndexName}' on (TenantId, Name).", IdentityRoleIndexes.TenantIdNameUnique);
                break;

            case TenantRoleNameIndexState.Weak:
                // If a store-wide unique Name index still protects role names, keep it and warn. Otherwise nothing
                // enforces role name uniqueness, so refuse to start rather than let a tenant save duplicate role names.
                var weakIndexName = GetIndexName(stateIndex!);
                var storeWideIndex = existingIndexes.FirstOrDefault(IsStoreWideUniqueNameIndex);

                if (storeWideIndex == null)
                {
                    throw new InvalidOperationException(
                        $"The role index '{weakIndexName}' on (TenantId, Name) is not a plain unique index, and no other index keeps role names unique. " +
                        $"Drop '{weakIndexName}' (and remove any duplicate role names within a tenant), then restart so the unique index '{IdentityRoleIndexes.TenantIdNameUnique}' can be created.");
                }

                logger.LogWarning(
                    "The role index '{IndexName}' on (TenantId, Name) is not a plain unique index, so the store-wide unique index '{LegacyIndexName}' is kept and role names stay unique across tenants. Drop '{IndexName}' and restart to make role names unique per tenant.",
                    weakIndexName,
                    GetIndexName(storeWideIndex),
                    weakIndexName);
                await CreateTenantIdIndexAsync(collection, indexBuilder, cancellationToken);
                return;
        }

        await DropLegacyNameIndexAsync(collection, existingIndexes, logger, cancellationToken);
        WarnAboutOtherStoreWideNameIndexes(existingIndexes, logger);
        await CreateTenantIdIndexAsync(collection, indexBuilder, cancellationToken);
    }

    private static (TenantRoleNameIndexState State, BsonDocument? Index) GetTenantRoleNameIndexState(IReadOnlyCollection<BsonDocument> existingIndexes)
    {
        // Any unique index on (TenantId, Name) enforces per-tenant uniqueness, whatever the key directions. Only an
        // ascending one blocks creating TenantId_1_Name_1, because MongoDB refuses a second index with the same keys.
        var unique = existingIndexes.FirstOrDefault(x => HasTenantIdNameKey(x, ascendingOnly: false) && IsPlainUnique(x));

        if (unique != null)
        {
            return (TenantRoleNameIndexState.Unique, unique);
        }

        var weak = existingIndexes.FirstOrDefault(x => HasTenantIdNameKey(x, ascendingOnly: true));
        return weak == null ? (TenantRoleNameIndexState.Missing, null) : (TenantRoleNameIndexState.Weak, weak);
    }

    private static async Task DropLegacyNameIndexAsync(IMongoCollection<Role> collection, IEnumerable<BsonDocument> existingIndexes, ILogger logger, CancellationToken cancellationToken)
    {
        if (!existingIndexes.Any(x => GetIndexName(x) == IdentityRoleIndexes.LegacyNameUnique))
        {
            logger.LogDebug("Role unique index '{IndexName}' was not found.", IdentityRoleIndexes.LegacyNameUnique);
            return;
        }

        try
        {
            await collection.Indexes.DropOneAsync(IdentityRoleIndexes.LegacyNameUnique, cancellationToken);
            logger.LogInformation("Dropped the store-wide role unique index '{IndexName}'; role names are now unique per tenant through '{TenantIndexName}'.", IdentityRoleIndexes.LegacyNameUnique, IdentityRoleIndexes.TenantIdNameUnique);
        }
        catch (MongoCommandException exception) when (IsIndexNotFound(exception))
        {
            logger.LogDebug("Role unique index '{IndexName}' was already dropped.", IdentityRoleIndexes.LegacyNameUnique);
        }
    }

    /// <summary>
    /// Only <c>Name_1</c> is dropped automatically. A store-wide unique Name index under another name was created by
    /// someone else, so it is left alone, but it keeps role names unique across tenants, so say so.
    /// </summary>
    private static void WarnAboutOtherStoreWideNameIndexes(IEnumerable<BsonDocument> existingIndexes, ILogger logger)
    {
        foreach (var index in existingIndexes.Where(x => IsStoreWideUniqueNameIndex(x) && GetIndexName(x) != IdentityRoleIndexes.LegacyNameUnique))
        {
            logger.LogWarning(
                "The role index '{IndexName}' keeps role names unique across the whole store, so tenants still cannot share a role name. Drop '{IndexName}' to make role names unique per tenant through '{TenantIndexName}'.",
                GetIndexName(index),
                GetIndexName(index),
                IdentityRoleIndexes.TenantIdNameUnique);
        }
    }

    private static Task CreateTenantIdIndexAsync(IMongoCollection<Role> collection, IndexKeysDefinitionBuilder<Role> indexBuilder, CancellationToken cancellationToken) =>
        collection.Indexes.CreateOneAsync(new CreateIndexModel<Role>(indexBuilder.Ascending(x => x.TenantId)), cancellationToken: cancellationToken);

    private static bool IsStoreWideUniqueNameIndex(BsonDocument index) => HasNameOnlyKey(index) && IsPlainUnique(index);

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
    /// Whether the index keys are exactly TenantId, then Name: ascending only, or in any direction.
    /// </summary>
    private static bool HasTenantIdNameKey(BsonDocument index, bool ascendingOnly)
    {
        if (!index.TryGetValue("key", out var key) || key is not BsonDocument keyDocument || keyDocument.ElementCount != 2)
        {
            return false;
        }

        return IsKey(keyDocument.GetElement(0), nameof(Role.TenantId), ascendingOnly) && IsKey(keyDocument.GetElement(1), nameof(Role.Name), ascendingOnly);
    }

    /// <summary>
    /// Whether the index keys are exactly Name, in either direction. Like the legacy store-wide index, a unique one keeps
    /// role names unique across the store.
    /// </summary>
    private static bool HasNameOnlyKey(BsonDocument index) =>
        index.TryGetValue("key", out var key) && key is BsonDocument keyDocument && keyDocument.ElementCount == 1 && IsKey(keyDocument.GetElement(0), nameof(Role.Name), ascendingOnly: false);

    private static bool IsKey(BsonElement element, string field, bool ascendingOnly) =>
        string.Equals(element.Name, field, StringComparison.Ordinal)
        && element.Value.IsNumeric
        && (ascendingOnly ? element.Value.ToDouble() > 0 : element.Value.ToDouble() != 0);

    /// <summary>
    /// Whether the index is unique over every document, rather than non-unique or limited by a partial filter.
    /// </summary>
    private static bool IsPlainUnique(BsonDocument index) =>
        index.TryGetValue("unique", out var unique) && unique.ToBoolean() && !index.Contains("partialFilterExpression");
}
