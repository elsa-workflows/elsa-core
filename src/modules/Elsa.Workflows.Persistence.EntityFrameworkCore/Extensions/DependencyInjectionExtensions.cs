using Elsa.Workflows.Persistence.Configuration;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Configuration;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowPersistenceConfigurator UseEntityFrameworkCore(this WorkflowPersistenceConfigurator configurator, Action<EFCoreWorkflowPersistenceConfigurator> configure)
    {
        configurator.ServiceConfiguration.Configure(configure);
        return configurator;
    }
}