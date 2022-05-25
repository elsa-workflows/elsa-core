using Elsa.Workflows.Persistence.EntityFrameworkCore.Options;
using Elsa.Workflows.Persistence.Options;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowPersistenceOptions UseEntityFrameworkCoreProvider(this WorkflowPersistenceOptions configurator, Action<EFCoreWorkflowPersistenceOptions> configure)
    {
        configurator.ElsaOptionsConfigurator.Configure(() => new EFCoreWorkflowPersistenceOptions(configurator), configure);
        return configurator;
    }
}