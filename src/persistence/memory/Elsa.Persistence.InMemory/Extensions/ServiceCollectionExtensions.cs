using Elsa.Options;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Options;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS8631

namespace Elsa.Persistence.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static PersistenceOptions UseInMemoryProvider(this PersistenceOptions configurator)
    {
        var services = configurator.ElsaOptionsConfigurator.Services;

        services
            .AddSingleton<InMemoryStore<WorkflowDefinition>>()
            .AddSingleton<InMemoryStore<WorkflowInstance>>()
            .AddSingleton<InMemoryStore<WorkflowBookmark>>()
            .AddSingleton<InMemoryStore<WorkflowTrigger>>()
            .AddSingleton<InMemoryStore<WorkflowExecutionLogRecord>>()
            .AddSingleton<InMemoryWorkflowDefinitionStore>()
            .AddSingleton<InMemoryWorkflowInstanceStore>()
            .AddSingleton<InMemoryWorkflowBookmarkStore>()
            .AddSingleton<InMemoryWorkflowTriggerStore>()
            .AddSingleton<InMemoryWorkflowExecutionLogStore>()
            ;

        return configurator.ElsaOptionsConfigurator.Configure(() => new PersistenceOptions(configurator.ElsaOptionsConfigurator), o => o
                .WithWorkflowDefinitionStore(sp => sp.GetRequiredService<InMemoryWorkflowDefinitionStore>())
                .WithWorkflowInstanceStore(sp => sp.GetRequiredService<InMemoryWorkflowInstanceStore>())
                .WithWorkflowBookmarkStore(sp => sp.GetRequiredService<InMemoryWorkflowBookmarkStore>())
                .WithWorkflowTriggerStore(sp => sp.GetRequiredService<InMemoryWorkflowTriggerStore>())
                .WithWorkflowExecutionLogStore(sp => sp.GetRequiredService<InMemoryWorkflowExecutionLogStore>()))
            ;
    }
}