using Elsa.Options;
using Elsa.Persistence.Implementations;
using Elsa.Persistence.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Options;

public class PersistenceOptions : IConfigurator
{
    public ElsaOptionsConfigurator ElsaOptionsConfigurator { get; }

    public PersistenceOptions(ElsaOptionsConfigurator elsaOptionsConfigurator)
    {
        ElsaOptionsConfigurator = elsaOptionsConfigurator;
    }
    
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = _ => new NullWorkflowDefinitionStore();
    public Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = _ => new NullWorkflowInstanceStore();
    public Func<IServiceProvider, IWorkflowBookmarkStore> WorkflowBookmarkStore { get; set; } = _ => new NullWorkflowBookmarkStore();
    public Func<IServiceProvider, IWorkflowTriggerStore> WorkflowTriggerStore { get; set; } = _ => new NullWorkflowTriggerStore();
    public Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStore { get; set; } = _ => new NullWorkflowExecutionLogStore();

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

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton(WorkflowDefinitionStore)
            .AddSingleton(WorkflowInstanceStore)
            .AddSingleton(WorkflowBookmarkStore)
            .AddSingleton(WorkflowTriggerStore)
            .AddSingleton(WorkflowExecutionLogStore)
            ;
    }
}