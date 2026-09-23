using Elsa.Connections.Features;
using Elsa.Connections.Persistence.EFCore.Features;

namespace Elsa.Connections.Persistence.EFCore.Extensions;

public static class ConnectionsFeatureExtensions
{
    public static ConnectionsFeature UseEntityFrameworkCore(this ConnectionsFeature feature, Action<EFCoreConnectionsPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
