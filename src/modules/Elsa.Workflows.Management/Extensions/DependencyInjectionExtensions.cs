using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core.Configuration;
using Elsa.Workflows.Management.Configuration;

namespace Elsa.Workflows.Management.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseManagement(this IServiceConfiguration serviceConfiguration, Action<WorkflowManagementConfigurator>? configure = default)
    {
        serviceConfiguration.Configure(configure);
        return serviceConfiguration;
    }
}