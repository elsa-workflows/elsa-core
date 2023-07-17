using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.Features;

/// <summary>
/// Enables functionality to tag workflows with labels.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
public class LabelsFeature : FeatureBase
{
    /// <inheritdoc />
    public LabelsFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// A delegate that provides an instance of an implementation of <see cref="ILabelStore"/>.
    /// </summary>
    public Func<IServiceProvider, ILabelStore> LabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryLabelStore>();
    
    /// <summary>
    /// A delegate that provides an instance of an implementation of <see cref="IWorkflowDefinitionLabelStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDefinitionLabelStore> WorkflowDefinitionLabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryWorkflowDefinitionLabelStore>();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
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