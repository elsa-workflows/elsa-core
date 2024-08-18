using Elsa.Agents.Persistence.Features;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

/// <summary>
/// Provides extensions to the <see cref="AlterationsFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="AgentPersistenceFeature"/> to use EF Core persistence providers.
    /// </summary>
    public static AgentPersistenceFeature UseEntityFrameworkCore(this AgentPersistenceFeature feature, Action<EFCoreAgentPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}