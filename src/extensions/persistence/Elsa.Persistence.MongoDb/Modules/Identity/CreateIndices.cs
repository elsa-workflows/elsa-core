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
                var existingNames = await ListIndexNamesAsync(collection, cancellationToken);

                if (existingNames.Contains(IdentityRoleIndexes.TenantIdNameUnique))
                {
                    logger.LogDebug("Role unique index '{IndexName}' is already present.", IdentityRoleIndexes.TenantIdNameUnique);
                }
                else
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

    private static async Task<HashSet<string>> ListIndexNamesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        using var cursor = await collection.Indexes.ListAsync(cancellationToken);

        foreach (var index in await cursor.ToListAsync(cancellationToken))
        {
            if (index.TryGetValue("name", out var name) && name.BsonType == BsonType.String)
            {
                names.Add(name.AsString);
            }
        }

        return names;
    }
}
