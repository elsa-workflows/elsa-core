using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Distributed.Handlers;
using Elsa.Workflows.Runtime.Distributed.Services;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.Features;

/// <summary>
/// Installs and configures workflow runtime features.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class DistributedRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public DistributedRuntimeFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.UseWorkflowRuntime(runtime =>
        {
            runtime.WorkflowRuntime = sp => sp.GetRequiredService<DistributedWorkflowRuntime>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<DistributedWorkflowRuntime>();
        Services.AddCommandHandler<CancelWorkflowsCommandHandler>();
    }
}