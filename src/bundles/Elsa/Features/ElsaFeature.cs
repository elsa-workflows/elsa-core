using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Features;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Features;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Features;

/// <summary>
/// Represents Elsa as a feature of the system.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(FlowchartFeature))]
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(WorkflowManagementFeature))]
public class ElsaFeature : FeatureBase
{
    /// <summary>
    /// Set this to true to opt out of automatically registering activities from Elsa.Workflows.Core.
    /// </summary>
    public bool DisableAutomaticActivityRegistration { get; set; }
    
    /// <inheritdoc />
    public ElsaFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.UseWorkflowManagement(management => management.AddActivitiesFrom<WriteLine>());
    }
}