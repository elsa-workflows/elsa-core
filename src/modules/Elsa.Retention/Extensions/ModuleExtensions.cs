using Elsa.Features.Services;
using Elsa.Retention.Feature;
using JetBrains.Annotations;

namespace Elsa.Retention.Extensions;

/// <summary>
/// Provides extensions to install the <see cref="RetentionFeature" /> feature.
/// </summary>
[UsedImplicitly]
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the <see cref="RetentionFeature" /> feature.
    /// </summary>
    public static IModule UseRetention(this IModule module, Action<RetentionFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}