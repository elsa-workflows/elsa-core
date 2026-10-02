using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;

namespace Elsa.WorkflowContexts.Features;

/// <summary>
/// A feature that adds support for workflow context providers.
/// </summary>
[UsedImplicitly]
[DependsOn(typeof(WorkflowManagementFeature))]
public class WorkflowContextsFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowContextsFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<WorkflowContextsFeature>();
        Module.AddFastEndpointsAssembly(GetType());
    }
}