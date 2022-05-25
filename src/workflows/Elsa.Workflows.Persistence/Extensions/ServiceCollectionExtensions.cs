using Elsa.Persistence.Common.Extensions;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Implementations;
using Elsa.Workflows.Persistence.Options;

namespace Elsa.Workflows.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static ElsaOptionsConfigurator ConfigurePersistence(this ElsaOptionsConfigurator configurator, Action<PersistenceOptions>? configure = default)
    {
        var services = configurator.Services;

        services
            .AddMemoryStore<WorkflowDefinition, MemoryWorkflowDefinitionStore>()
            .AddMemoryStore<WorkflowInstance, MemoryWorkflowInstanceStore>()
            .AddMemoryStore<WorkflowBookmark, MemoryWorkflowBookmarkStore>()
            .AddMemoryStore<WorkflowTrigger, MemoryWorkflowTriggerStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()
            ;

        configurator.Configure(() => new PersistenceOptions(configurator), configure);
        return configurator;
    }
}