using Elsa.DataSets.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IModule"/> to configure the DataSets feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the DataSets feature.
    /// </summary>
    public static IModule UseDataSets(this IModule module, Action<DataSetFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}