using Elsa.Agents.Persistence.EntityFrameworkCore;
using Elsa.Agents.Persistence.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Agents;

/// <summary>
/// Provides extensions to the <see cref="AgentPersistenceFeature"/> feature.
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