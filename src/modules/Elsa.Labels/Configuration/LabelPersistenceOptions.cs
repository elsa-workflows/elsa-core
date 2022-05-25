using Elsa.Labels.Entities;
using Elsa.Labels.Implementations;
using Elsa.Labels.Services;
using Elsa.Persistence.Common.Extensions;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.Configuration;

public class LabelPersistenceOptions : ConfiguratorBase
{
    public LabelPersistenceOptions(IServiceConfiguration serviceConfiguration)
    {
        ServiceConfiguration = serviceConfiguration;
    }
    
    public IServiceConfiguration ServiceConfiguration { get; }
    
    public Func<IServiceProvider, ILabelStore> LabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryLabelStore>();
    public Func<IServiceProvider, IWorkflowDefinitionLabelStore> WorkflowDefinitionLabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryWorkflowDefinitionLabelStore>();
    
    public LabelPersistenceOptions WithLabelStore(Func<IServiceProvider, ILabelStore> factory)
    {
        LabelStore = factory;
        return this;
    }
    
    public LabelPersistenceOptions WithWorkflowDefinitionLabelStore(Func<IServiceProvider, IWorkflowDefinitionLabelStore> factory)
    {
        WorkflowDefinitionLabelStore = factory;
        return this;
    }

    public override void ConfigureServices(IServiceConfiguration configuration)
    {
        configuration.Services
            .AddMemoryStore<Label, InMemoryLabelStore>()
            .AddMemoryStore<WorkflowDefinitionLabel, InMemoryWorkflowDefinitionLabelStore>()
            .AddSingleton(LabelStore)
            .AddSingleton(WorkflowDefinitionLabelStore)
            ;
    }
}