using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Configures and enables <see cref="BackgroundWorkflowStateExporter"/>.
/// </summary>
[DependsOn(typeof(WorkflowInstancesFeature))]
[PublicAPI]
public class DefaultWorkflowStateExporterFeature : FeatureBase
{
    /// <inheritdoc />
    public DefaultWorkflowStateExporterFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(workflowRuntime => workflowRuntime.WorkflowStateExporter = sp => ActivatorUtilities.CreateInstance<DefaultWorkflowStateExporter>(sp));
        Services.AddCommandHandler<ExportWorkflowStateToDbCommandHandler, ExportWorkflowStateToDbCommand>();
    }
}