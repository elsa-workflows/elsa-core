using Elsa.Alterations.Core.Features;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// Provides extensions to the <see cref="CoreAlterationsFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="CoreAlterationsFeature"/> to use EF Core persistence providers.
    /// </summary>
    public static CoreAlterationsFeature UseEntityFrameworkCore(this CoreAlterationsFeature feature, Action<EFCoreAlterationPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}