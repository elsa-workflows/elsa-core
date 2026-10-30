using Elsa.Features.Services;
using Elsa.Persistence.MongoDb.Features;
using Elsa.Persistence.MongoDb.Options;
using JetBrains.Annotations;

namespace Elsa.Persistence.MongoDb.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> to configure the <see cref="MongoDbFeature"/> feature.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the <see cref="MongoDbFeature"/> feature.
    /// </summary>
    public static IModule UseMongoDb(
        this IModule module, 
        string connectionString,
        Action<MongoDbOptions>? options = null,
        Action<MongoDbFeature>? configure = null)
    {
        configure += f => f.ConnectionString = connectionString;
        configure += f => f.Options += options;
        module.Configure(configure);
        return module;
    }
}