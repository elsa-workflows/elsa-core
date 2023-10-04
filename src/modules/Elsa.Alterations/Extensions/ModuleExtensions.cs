using Elsa.Alterations.Features;
using Elsa.Extensions;
using Elsa.Features.Services;

namespace Elsa.Alterations.Extensions;

/// <summary>
/// Adds the <see cref="AlterationsFeature"/>.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the <see cref="AlterationsFeature"/>.
    /// </summary>
    public static IModule UseAlterations(this IModule module, Action<AlterationsFeature>? configure = default)
    {
        return module.Use(configure);
    }
}