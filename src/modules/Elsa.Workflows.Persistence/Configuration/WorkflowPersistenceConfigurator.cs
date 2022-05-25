using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Persistence.Implementations;
using Elsa.Workflows.Persistence.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Persistence.Configuration;

public class WorkflowPersistenceConfigurator : ConfiguratorBase
{
    public WorkflowPersistenceConfigurator(IServiceConfiguration serviceConfiguration)
    {
        ServiceConfiguration = serviceConfiguration;
    }

    public IServiceConfiguration ServiceConfiguration { get; }
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();
    public Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>();
    public Func<IServiceProvider, IWorkflowBookmarkStore> WorkflowBookmarkStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowBookmarkStore>();
    public Func<IServiceProvider, IWorkflowTriggerStore> WorkflowTriggerStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowTriggerStore>();
    public Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();

    public WorkflowPersistenceConfigurator WithWorkflowDefinitionStore(Func<IServiceProvider, IWorkflowDefinitionStore> factory)
    {
        WorkflowDefinitionStore = factory;
        return this;
    }

    public WorkflowPersistenceConfigurator WithWorkflowInstanceStore(Func<IServiceProvider, IWorkflowInstanceStore> factory)
    {
        WorkflowInstanceStore = factory;
        return this;
    }

    public WorkflowPersistenceConfigurator WithWorkflowBookmarkStore(Func<IServiceProvider, IWorkflowBookmarkStore> factory)
    {
        WorkflowBookmarkStore = factory;
        return this;
    }

    public WorkflowPersistenceConfigurator WithWorkflowTriggerStore(Func<IServiceProvider, IWorkflowTriggerStore> factory)
    {
        WorkflowTriggerStore = factory;
        return this;
    }

    public WorkflowPersistenceConfigurator WithWorkflowExecutionLogStore(Func<IServiceProvider, IWorkflowExecutionLogStore> factory)
    {
        WorkflowExecutionLogStore = factory;
        return this;
    }

    public override void ConfigureServices(IServiceCollection services)
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