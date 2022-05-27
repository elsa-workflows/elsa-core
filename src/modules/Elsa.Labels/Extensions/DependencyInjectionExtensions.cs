using Elsa.Features.Services;
using Elsa.Labels.Features;

namespace Elsa.Labels.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseLabels(this IModule configuration, Action<LabelsFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}