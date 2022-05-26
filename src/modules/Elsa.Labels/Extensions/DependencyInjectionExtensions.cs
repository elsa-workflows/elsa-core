using Elsa.Labels.Configuration;
using Elsa.ServiceConfiguration.Services;

namespace Elsa.Labels.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseLabels(this IServiceConfiguration configuration, Action<LabelsConfigurator>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}