using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core.Configuration;

namespace Elsa.Workflows.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseWorkflows(this IServiceConfiguration configuration, Action<WorkflowConfigurator>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}