using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseWorkflows(this IServiceConfiguration configuration, Action<WorkflowConfigurator>? configure = default)
    {
        configuration.Configure(() => new WorkflowConfigurator(configuration), configure);
        return configuration;
    }
}