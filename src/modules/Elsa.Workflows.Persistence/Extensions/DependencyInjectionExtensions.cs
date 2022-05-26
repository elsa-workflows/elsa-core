using Elsa.Persistence.Common.Extensions;
using Elsa.Workflows.Core.Configuration;
using Elsa.Workflows.Persistence.Configuration;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Implementations;

namespace Elsa.Workflows.Persistence.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowConfigurator UsePersistence(this WorkflowConfigurator configurator, Action<WorkflowPersistenceConfigurator>? configure = default)
    {
        var configuration = configurator.ServiceConfiguration;
        var services = configuration.Services;

        services
            .AddMemoryStore<WorkflowDefinition, MemoryWorkflowDefinitionStore>()
            .AddMemoryStore<WorkflowInstance, MemoryWorkflowInstanceStore>()
            .AddMemoryStore<WorkflowBookmark, MemoryWorkflowBookmarkStore>()
            .AddMemoryStore<WorkflowTrigger, MemoryWorkflowTriggerStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()
            ;

        configuration.Configure(() => new WorkflowPersistenceConfigurator(configuration), configure);
        return configurator;
    }
}