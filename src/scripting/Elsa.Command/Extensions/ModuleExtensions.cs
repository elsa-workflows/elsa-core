using Elsa.Command.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="IModule"/> that installs the <see cref="CommandFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Setup the <see cref="CommandFeature"/> feature.
    /// </summary>
    public static IModule UseCommandExecuter(this IModule module, Action<CommandFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}