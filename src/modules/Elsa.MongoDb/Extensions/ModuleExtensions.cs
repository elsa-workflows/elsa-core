using Elsa.Features.Services;
using Elsa.MongoDb.Features;
using JetBrains.Annotations;

namespace Elsa.MongoDb.Extensions;

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
        Action<MongoDbFeature>? configure = default)
    {
        configure += f => f.ConnectionString = connectionString;
        module.Configure(configure);
        return module;
    }
}