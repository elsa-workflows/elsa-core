using Elsa.Environments.Features;
using Elsa.Features.Services;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IModule"/> to add environment services.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Adds support for environments.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The module.</returns>
    public static IModule UseEnvironments(this IModule module, Action<EnvironmentsFeature>? configure = default)
    {
        module.Use(configure);
        return module;
    }
}