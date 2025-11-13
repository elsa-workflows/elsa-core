using Elsa.Dsl.ElsaScript.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IModule"/>.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the ElsaScript DSL feature.
    /// </summary>
    public static IModule UseElsaScript(this IModule module, Action<ElsaScriptFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
