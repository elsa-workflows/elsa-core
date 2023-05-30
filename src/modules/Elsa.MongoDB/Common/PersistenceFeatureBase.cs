using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDB.Common;

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
    /// Registers an <see cref="MongoStore{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    /// <typeparam name="TDocument">The document type of the store.</typeparam>
    protected void AddStore<TDocument, TStore>() where TDocument : class where TStore : class
    {
        Services
            .AddSingleton<MongoStore<TDocument>>()
            .AddSingleton<TStore>()
            ;
    }
}