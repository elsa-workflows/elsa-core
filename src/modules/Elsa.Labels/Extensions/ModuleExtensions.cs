using Elsa.Features.Services;
using Elsa.Labels.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseLabels(this IModule configuration, Action<LabelsFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}