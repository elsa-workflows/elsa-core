using Elsa.Labels.Entities;
using Elsa.Labels.Implementations;
using Elsa.Labels.Services;
using Elsa.Persistence.Common.Extensions;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.Configuration;

public class LabelsConfigurator : ConfiguratorBase
{
    public LabelsConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }
    
    public Func<IServiceProvider, ILabelStore> LabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryLabelStore>();
    public Func<IServiceProvider, IWorkflowDefinitionLabelStore> WorkflowDefinitionLabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryWorkflowDefinitionLabelStore>();
    
    public LabelsConfigurator WithLabelStore(Func<IServiceProvider, ILabelStore> factory)
    {
        LabelStore = factory;
        return this;
    }
    
    public LabelsConfigurator WithWorkflowDefinitionLabelStore(Func<IServiceProvider, IWorkflowDefinitionLabelStore> factory)
    {
        WorkflowDefinitionLabelStore = factory;
        return this;
    }

    public override void Apply()
    {
        Services
            .AddMemoryStore<Label, InMemoryLabelStore>()
            .AddMemoryStore<WorkflowDefinitionLabel, InMemoryWorkflowDefinitionLabelStore>()
            .AddSingleton(LabelStore)
            .AddSingleton(WorkflowDefinitionLabelStore)
            ;
    }
}