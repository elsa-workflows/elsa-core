using Elsa.Mediator.Extensions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Handlers.Commands;
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
                .AddInMemoryHandlers()
                .AddSingleton<InMemoryStore<WorkflowDefinition>>()
                .AddSingleton<InMemoryStore<WorkflowInstance>>()
                .AddSingleton<InMemoryStore<WorkflowBookmark>>()
                .AddSingleton<InMemoryStore<WorkflowTrigger>>()
                .AddSingleton<InMemoryStore<WorkflowExecutionLogRecord>>()
                .AddSingleton<IWorkflowInstanceStore, InMemoryWorkflowInstanceStore>()
            ;
    }

    public static IServiceCollection AddInMemoryHandlers(this IServiceCollection services) => services.AddHandlersFrom<SaveWorkflowDefinitionHandler>();
}