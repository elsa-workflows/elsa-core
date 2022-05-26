using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Persistence.Configuration;

namespace Elsa.Workflows.Persistence.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UsePersistence(this IServiceConfiguration serviceConfiguration, Action<WorkflowPersistenceConfigurator>? configure = default )
    {
        serviceConfiguration.Configure(configure);
        return serviceConfiguration;
    }
}