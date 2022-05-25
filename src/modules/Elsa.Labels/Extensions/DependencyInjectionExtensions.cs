using Elsa.Labels.Configuration;
using Elsa.ServiceConfiguration.Services;

namespace Elsa.Labels.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration AddLabels(this IServiceConfiguration configuration, Action<LabelPersistenceOptions>? configure = default)
    {
        configuration.Configure(() => new LabelPersistenceOptions(configuration), configure);
        return configuration;
    }
}