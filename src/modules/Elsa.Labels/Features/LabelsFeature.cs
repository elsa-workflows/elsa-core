using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Implementations;
using Elsa.Labels.Services;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Elsa.Persistence.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.Features;

[DependsOn(typeof(MediatorFeature))]
public class LabelsFeature : FeatureBase
{
    public LabelsFeature(IModule module) : base(module)
    {
    }
    
    public Func<IServiceProvider, ILabelStore> LabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryLabelStore>();
    public Func<IServiceProvider, IWorkflowDefinitionLabelStore> WorkflowDefinitionLabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryWorkflowDefinitionLabelStore>();
    
    public LabelsFeature WithLabelStore(Func<IServiceProvider, ILabelStore> factory)
    {
        LabelStore = factory;
        return this;
    }
    
    public LabelsFeature WithWorkflowDefinitionLabelStore(Func<IServiceProvider, IWorkflowDefinitionLabelStore> factory)
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

        Services.AddNotificationHandlersFrom(GetType());
    }
}