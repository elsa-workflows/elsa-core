using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Connections.Persistence.Features;
using Elsa.Features.Services;


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
