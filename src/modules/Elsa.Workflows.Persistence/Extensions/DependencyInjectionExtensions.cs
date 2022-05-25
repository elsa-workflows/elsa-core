using Elsa.Persistence.Common.Extensions;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Implementations;
using Elsa.Workflows.Persistence.Options;

namespace Elsa.Workflows.Persistence.Extensions;

public static class DependencyInjectionExtensions
{
    public static ElsaOptionsConfigurator ConfigureWorkflowPersistence(this ElsaOptionsConfigurator configurator, Action<WorkflowPersistenceOptions>? configure = default)
    {
        var services = configurator.Services;

        services
            .AddMemoryStore<WorkflowDefinition, MemoryWorkflowDefinitionStore>()
            .AddMemoryStore<WorkflowInstance, MemoryWorkflowInstanceStore>()
            .AddMemoryStore<WorkflowBookmark, MemoryWorkflowBookmarkStore>()
            .AddMemoryStore<WorkflowTrigger, MemoryWorkflowTriggerStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()
            ;

        configurator.Configure(() => new WorkflowPersistenceOptions(configurator), configure);
        return configurator;
    }
}