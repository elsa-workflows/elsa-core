using Elsa.Bpmn.Activities;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;

namespace Elsa.Bpmn.Features;

/// <summary>
/// Provides BPMN execution support to the system.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
public class BpmnFeature : FeatureBase
{
    /// <inheritdoc />
    public BpmnFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Module.AddActivity<BpmnProcess>();
    }
}
