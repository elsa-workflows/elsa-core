using Elsa.Features.Services;
using Elsa.Python.Features;
using Elsa.Python.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="IModule"/> that installs the <see cref="PythonFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Setup the <see cref="PythonFeature"/> feature.
    /// </summary>
    public static IModule UsePython(this IModule module, Action<PythonFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
    
    /// <summary>
    /// Setup the <see cref="PythonFeature"/> feature.
    /// </summary>
    public static IModule UsePython(this IModule module, Action<PythonOptions> configureOptions)
    {
        return module.UsePython(python => python.PythonOptions += configureOptions);
    }
}