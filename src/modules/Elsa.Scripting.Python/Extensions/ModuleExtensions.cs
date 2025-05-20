using Elsa.Features.Services;
using Elsa.Scripting.Python.Features;

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
}