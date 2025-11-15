using Elsa.Expressions.Xs.Features;
using Elsa.Expressions.Xs.Options;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="IModule"/> that installs the <see cref="XsFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Setup the <see cref="XsFeature"/> feature.
    /// </summary>
    public static IModule UseXs(this IModule module, Action<XsFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
    
    /// <summary>
    /// Setup the <see cref="XsFeature"/> feature.
    /// </summary>
    public static IModule UseXs(this IModule module, Action<XsOptions> configureOptions)
    {
        return module.UseXs(xs => xs.XsOptionsConfig = configureOptions);
    }
}
