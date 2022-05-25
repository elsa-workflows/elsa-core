using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Persistence.Implementations;
using Elsa.Workflows.Persistence.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Persistence.Options;

public class PersistenceOptions : ConfiguratorBase
{
    public ElsaOptionsConfigurator ElsaOptionsConfigurator { get; }

    public PersistenceOptions(ElsaOptionsConfigurator elsaOptionsConfigurator)
    {
        ElsaOptionsConfigurator = elsaOptionsConfigurator;
    }
    
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();
    public Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>();
    public Func<IServiceProvider, IWorkflowBookmarkStore> WorkflowBookmarkStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowBookmarkStore>();
    public Func<IServiceProvider, IWorkflowTriggerStore> WorkflowTriggerStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowTriggerStore>();
    public Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();

    public PersistenceOptions WithWorkflowDefinitionStore(Func<IServiceProvider, IWorkflowDefinitionStore> factory)
    {
        WorkflowDefinitionStore = factory;
        return this;
    }

    public PersistenceOptions WithWorkflowInstanceStore(Func<IServiceProvider, IWorkflowInstanceStore> factory)
    {
        WorkflowInstanceStore = factory;
        return this;
    }

    public PersistenceOptions WithWorkflowBookmarkStore(Func<IServiceProvider, IWorkflowBookmarkStore> factory)
    {
        WorkflowBookmarkStore = factory;
        return this;
    }

    public PersistenceOptions WithWorkflowTriggerStore(Func<IServiceProvider, IWorkflowTriggerStore> factory)
    {
        WorkflowTriggerStore = factory;
        return this;
    }
    
    public PersistenceOptions WithWorkflowExecutionLogStore(Func<IServiceProvider, IWorkflowExecutionLogStore> factory)
    {
        WorkflowExecutionLogStore = factory;
        return this;
    }
    
    public override void ConfigureServices(ElsaOptionsConfigurator configurator)
    {
        configurator.Services
            .AddSingleton(WorkflowDefinitionStore)
            .AddSingleton(WorkflowInstanceStore)
            .AddSingleton(WorkflowBookmarkStore)
            .AddSingleton(WorkflowTriggerStore)
            .AddSingleton(WorkflowExecutionLogStore)
            ;
    }
}