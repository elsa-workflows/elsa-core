using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Configures and enables <see cref="BackgroundWorkflowStateExporter"/>.
/// </summary>
[DependsOn(typeof(WorkflowInstancesFeature))]
public class AsyncWorkflowStateExporterFeature : FeatureBase
{
    /// <inheritdoc />
    public AsyncWorkflowStateExporterFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(workflowRuntime => workflowRuntime.WorkflowStateExporter = sp => ActivatorUtilities.CreateInstance<BackgroundWorkflowStateExporter>(sp));
    }
}