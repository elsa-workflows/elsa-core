using Elsa.Features.Services;
using Elsa.Mediator.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IModule"/> that enable &amp; configure mediator specific features.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Adds and configures the <see cref="MediatorFeature"/> to the specified <see cref="IModule"/>.
    /// </summary>
    public static IModule AddMediator(this IModule module, Action<MediatorFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}