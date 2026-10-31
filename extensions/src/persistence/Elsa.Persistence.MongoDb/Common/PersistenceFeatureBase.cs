using Elsa.Common.Multitenancy;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Persistence.MongoDb.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Common;

/// <summary>
/// Base class for features that configure MongoDb persistence.
/// </summary>
public abstract class PersistenceFeatureBase : FeatureBase
{
    /// <inheritdoc />
    protected PersistenceFeatureBase(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Registers a <see cref="MongoDbStore{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    /// <typeparam name="TDocument">The document type of the store.</typeparam>
    protected void AddStore<TDocument, TStore>() where TDocument : class where TStore : class
    {
        // MongoDbStore scopes documents by tenant. Hosts without the multitenancy feature still resolve it, against the default tenant.
        Services.TryAddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        Services
            .AddScoped<MongoDbStore<TDocument>>()
            .AddScoped<TStore>();
    }

    /// <summary>
    /// Registers a <see cref="IMongoCollection{TDocument}"/>.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <typeparam name="TDocument">The document type of the collection.</typeparam>
    protected void AddCollection<TDocument>(string collectionName) where TDocument : class
    {
        Services.AddScoped(sp =>
        {
            var collectionNamingStrategy = sp.GetRequiredService<ICollectionNamingStrategy>();
            var formattedCollectionName = collectionNamingStrategy.GetCollectionName(collectionName);

            return sp.GetRequiredService<IMongoDatabase>()
                .GetCollection<TDocument>(formattedCollectionName);
        });
    }
}