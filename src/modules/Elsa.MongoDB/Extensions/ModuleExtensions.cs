using Elsa.Features.Services;
using Elsa.MongoDB.Features;
using Elsa.MongoDB.Options;
using JetBrains.Annotations;

namespace Elsa.MongoDB.Extensions;

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
        Action<MongoDbOptions> options,
        Action<MongoDbFeature>? configure = default)
    {
        configure += f => f.Options += options;
        module.Configure(configure);
        return module;
    }
}