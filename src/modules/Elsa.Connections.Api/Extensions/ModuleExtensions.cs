using Elsa.Connections.Api.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to Connection API endpoints.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the Semantic Kernel API feature.
    /// </summary>
    public static IModule UseConnectionsApi(this IModule module, Action<ConnectionsApiFeature>? configure = null)
    {
        return module.Use(configure);
    }
}