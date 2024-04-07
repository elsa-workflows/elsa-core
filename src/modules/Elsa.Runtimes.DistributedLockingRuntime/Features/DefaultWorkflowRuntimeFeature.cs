using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Runtimes.DistributedLockingRuntime.Services;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtimes.DistributedLockingRuntime.Features;

/// <summary>
/// Installs distributed locking runtime services.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class DefaultWorkflowRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public DefaultWorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>().WorkflowClient = sp => sp.GetRequiredService<DistributedLockingWorkflowClient>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<DistributedLockingWorkflowClient>();
    }
}