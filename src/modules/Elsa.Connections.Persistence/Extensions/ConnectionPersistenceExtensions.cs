using Elsa.Connections.Persistence.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to use Connection Persistence Feature.
/// </summary>
public static class ConnectionPersistenceExtensions
{
    /// <summary>
    /// Installs the persistence feature for the Connection module.
    /// </summary>
    public static IModule UseConnectionPersistence(this IModule module, Action<ConnectionPersistenceFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
