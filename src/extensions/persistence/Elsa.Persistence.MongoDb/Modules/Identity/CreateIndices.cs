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
            async (collection, indexBuilder) =>
            {
                // Role names are unique per tenant, not across the store (elsa-core#8615). The compound index is created
                // before the legacy store-wide Name_1 index is dropped, so the collection is never without name
                // uniqueness, and existing data always fits it because Name_1 was stricter. When several nodes start
                // together, creating the same index again is a no-op and a node that finds Name_1 already dropped
                // (IndexNotFound) carries on instead of failing to start.
                var existingIndexes = await ListIndexesAsync(collection, cancellationToken);
                var existingNames = existingIndexes.Select(GetIndexName).OfType<string>().ToHashSet(StringComparer.Ordinal);
                var tenantNameIndexes = existingIndexes.Where(HasTenantIdNameKey).ToList();

                if (tenantNameIndexes.Count == 0)
                {
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
                }
                else if (tenantNameIndexes.FirstOrDefault(IsPlainUnique) is { } replacement)
                {
                    logger.LogDebug("Role unique index '{IndexName}' on (TenantId, Name) is already present.", GetIndexName(replacement));
                }
                else
                {
                    // An index on (TenantId, Name) exists but does not enforce uniqueness on every row (it is not unique,
                    // or it is partial), and MongoDB will not create a second index with the same keys. If a store-wide
                    // unique Name index still protects role names, keep it and warn. Otherwise nothing enforces role name
                    // uniqueness, so refuse to start rather than let a tenant save duplicate role names.
                    var weakIndexName = GetIndexName(tenantNameIndexes[0]);
                    var legacyIndex = existingIndexes.FirstOrDefault(x => HasNameOnlyKey(x) && IsPlainUnique(x));

                    if (legacyIndex == null)
                    {
                        throw new InvalidOperationException(
                            $"The role index '{weakIndexName}' on (TenantId, Name) is not a plain unique index, and no other index keeps role names unique. " +
                            $"Drop '{weakIndexName}' (and remove any duplicate role names within a tenant), then restart so the unique index '{IdentityRoleIndexes.TenantIdNameUnique}' can be created.");
                    }

                    logger.LogWarning(
                        "The role index '{IndexName}' on (TenantId, Name) is not a plain unique index, so the store-wide unique index '{LegacyIndexName}' is kept and role names stay unique across tenants. Drop '{IndexName}' and restart to make role names unique per tenant.",
                        weakIndexName,
                        GetIndexName(legacyIndex),
                        weakIndexName);
                    await collection.Indexes.CreateOneAsync(new CreateIndexModel<Role>(indexBuilder.Ascending(x => x.TenantId)), cancellationToken: cancellationToken);
                    return;
                }

                if (existingNames.Contains(IdentityRoleIndexes.LegacyNameUnique))
                {
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
                else
                {
                    logger.LogDebug("Role unique index '{IndexName}' was not found.", IdentityRoleIndexes.LegacyNameUnique);
                }

                await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<Role>(indexBuilder.Ascending(x => x.TenantId)),
                    cancellationToken: cancellationToken);
            });
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
    /// Whether the index keys are exactly ascending TenantId, then ascending Name.
    /// </summary>
    private static bool HasTenantIdNameKey(BsonDocument index)
    {
        if (!index.TryGetValue("key", out var key) || key is not BsonDocument keyDocument || keyDocument.ElementCount != 2)
        {
            return false;
        }

        return IsAscending(keyDocument.GetElement(0), nameof(Role.TenantId)) && IsAscending(keyDocument.GetElement(1), nameof(Role.Name));
    }

    /// <summary>
    /// Whether the index keys are exactly ascending Name, like the legacy store-wide index.
    /// </summary>
    private static bool HasNameOnlyKey(BsonDocument index) =>
        index.TryGetValue("key", out var key) && key is BsonDocument keyDocument && keyDocument.ElementCount == 1 && IsAscending(keyDocument.GetElement(0), nameof(Role.Name));

    private static bool IsAscending(BsonElement element, string field) =>
        string.Equals(element.Name, field, StringComparison.Ordinal) && element.Value.IsNumeric && element.Value.ToDouble() > 0;

    /// <summary>
    /// Whether the index is unique over every document, rather than non-unique or limited by a partial filter.
    /// </summary>
    private static bool IsPlainUnique(BsonDocument index) =>
        index.TryGetValue("unique", out var unique) && unique.ToBoolean() && !index.Contains("partialFilterExpression");
}
