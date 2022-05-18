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
            .AddStore<WorkflowDefinition, InMemoryWorkflowDefinitionStore>()
            .AddStore<WorkflowInstance, InMemoryWorkflowInstanceStore>()
            .AddStore<WorkflowBookmark, InMemoryWorkflowBookmarkStore>()
            .AddStore<WorkflowTrigger, InMemoryWorkflowTriggerStore>()
            .AddStore<WorkflowExecutionLogRecord, InMemoryWorkflowExecutionLogStore>()
            .AddStore<Label, InMemoryLabelStore>()
            .AddStore<WorkflowDefinitionLabel, InMemoryWorkflowDefinitionLabelStore>()
            ;

        return configurator.ElsaOptionsConfigurator.Configure(() => new PersistenceOptions(configurator.ElsaOptionsConfigurator), o => o
            .WithWorkflowDefinitionStore(sp => sp.GetRequiredService<InMemoryWorkflowDefinitionStore>())
            .WithWorkflowInstanceStore(sp => sp.GetRequiredService<InMemoryWorkflowInstanceStore>())
            .WithWorkflowBookmarkStore(sp => sp.GetRequiredService<InMemoryWorkflowBookmarkStore>())
            .WithWorkflowTriggerStore(sp => sp.GetRequiredService<InMemoryWorkflowTriggerStore>())
            .WithWorkflowExecutionLogStore(sp => sp.GetRequiredService<InMemoryWorkflowExecutionLogStore>())
            .WithLabelStore(sp => sp.GetRequiredService<InMemoryLabelStore>())
            .WithWorkflowDefinitionLabelStore(sp => sp.GetRequiredService<InMemoryWorkflowDefinitionLabelStore>())
        );
    }

    private static IServiceCollection AddStore<TEntity, TStore>(this IServiceCollection services) where TEntity : Entity where TStore : class =>
        services
            .AddSingleton<InMemoryStore<TEntity>>()
            .AddSingleton<TStore>();
}