using Elsa.CSharp.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="IModule"/> that installs the <see cref="CSharpFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Setup the <see cref="CSharpFeature"/> feature.
    /// </summary>
    public static IModule UseCSharp(this IModule module, Action<CSharpFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}