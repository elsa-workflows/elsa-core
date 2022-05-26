using Elsa.Workflows.Persistence.Configuration;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Configuration;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowPersistenceConfigurator UseEntityFrameworkCore(this WorkflowPersistenceConfigurator configurator, Action<EFCoreWorkflowPersistenceOptions> configure)
    {
        configurator.ServiceConfiguration.Configure(() => new EFCoreWorkflowPersistenceOptions(configurator), configure);
        return configurator;
    }
}