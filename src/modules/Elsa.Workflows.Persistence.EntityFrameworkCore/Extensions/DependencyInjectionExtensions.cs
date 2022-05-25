using Elsa.Workflows.Persistence.Configuration;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Options;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowPersistenceConfigurator UseEntityFrameworkCoreProvider(this WorkflowPersistenceConfigurator configurator, Action<EFCoreWorkflowPersistenceOptions> configure)
    {
        configurator.ElsaOptionsConfigurator.Configure(() => new EFCoreWorkflowPersistenceOptions(configurator), configure);
        return configurator;
    }
}