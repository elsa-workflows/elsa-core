using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Runtimes.DistributedLockingRuntime.Features;

/// <summary>
/// Installs the default runtime services.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class DefaultWorkflowRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public DefaultWorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
    }
}