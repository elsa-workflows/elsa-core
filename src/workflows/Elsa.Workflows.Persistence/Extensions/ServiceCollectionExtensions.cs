using System.Reflection.Emit;
using Elsa.Options;
using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Implementations;
using Elsa.Persistence.Options;

namespace Elsa.Persistence.Extensions;

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