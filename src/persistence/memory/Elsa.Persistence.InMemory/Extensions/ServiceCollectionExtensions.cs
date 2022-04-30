using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Services;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS8631

namespace Elsa.Persistence.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
    {
        return services
                .AddSingleton<InMemoryStore<WorkflowDefinition>>()
                .AddSingleton<InMemoryStore<WorkflowInstance>>()
                .AddSingleton<InMemoryStore<WorkflowBookmark>>()
                .AddSingleton<InMemoryStore<WorkflowTrigger>>()
                .AddSingleton<InMemoryStore<WorkflowExecutionLogRecord>>()
                .AddSingleton<IWorkflowInstanceStore, InMemoryWorkflowInstanceStore>()
                .AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>()
                .AddSingleton<IWorkflowTriggerStore, InMemoryWorkflowTriggerStore>()
                .AddSingleton<IWorkflowBookmarkStore, InMemoryWorkflowBookmarkStore>()
                .AddSingleton<IWorkflowExecutionLogStore, InMemoryWorkflowExecutionLogStore>()
            ;
    }
}