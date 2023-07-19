using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Records execution logs for workflow instances.
/// </summary>
public class ExecutionLogRecordFeature : FeatureBase
{
    /// <inheritdoc />
    public ExecutionLogRecordFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddNotificationHandler<DeleteWorkflowExecutionLogRecords, WorkflowInstancesDeleting>();
    }
}