using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;
using Proto.Cluster;

namespace Elsa.ProtoActor.Handlers;

/// <summary>
/// Stops running workflows when their instances are deleted.
/// </summary>
[PublicAPI]
internal class StopRunningWorkflows : INotificationHandler<WorkflowInstancesDeleting>
{
    private readonly Cluster _cluster;

    /// <summary>
    /// Initializes a new instance of the <see cref="StopRunningWorkflows"/> class.
    /// </summary>
    public StopRunningWorkflows(Cluster cluster)
    {
        _cluster = cluster;
    }

    async Task INotificationHandler<WorkflowInstancesDeleting>.HandleAsync(WorkflowInstancesDeleting notification, CancellationToken cancellationToken)
    {
        foreach (var workflowInstanceId in notification.Ids)
        {
            var workflowGrainClient = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
            await workflowGrainClient.Stop(cancellationToken);
        }
    }
}