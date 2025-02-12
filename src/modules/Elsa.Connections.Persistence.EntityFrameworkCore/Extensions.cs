using Elsa.Connections.Persistence.Features;
using Elsa.Connections.Persistence.EntityFrameworkCore;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Agents;

/// <summary>
/// Provides extensions to the <see cref="ConnectionPersistenceFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="ConnectionPersistenceFeature"/> to use EF Core persistence providers.
    /// </summary>
    public static ConnectionPersistenceFeature UseEntityFrameworkCore(this ConnectionPersistenceFeature feature, Action<EFCoreConnectionPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}