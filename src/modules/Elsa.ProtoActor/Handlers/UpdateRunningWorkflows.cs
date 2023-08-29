using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Core.Notifications;
using JetBrains.Annotations;
using Proto.Cluster;
using WorkflowStatus = Elsa.Workflows.Core.WorkflowStatus;

namespace Elsa.ProtoActor.Handlers;

/// <summary>
/// Updates the <see cref="RunningWorkflows"/> with running workflow instances.
/// </summary>
[PublicAPI]
internal class UpdateRunningWorkflows : INotificationHandler<WorkflowExecuted>
{
    private readonly Cluster _cluster;

    public UpdateRunningWorkflows(Cluster cluster)
    {
        _cluster = cluster;
    }

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var client = _cluster.GetNamedRunningWorkflowsGrain();
        var workflowState = notification.WorkflowState;

        if (workflowState.Status == WorkflowStatus.Running)
        {
            var registerRequest = new RegisterRunningWorkflowRequest
            {
                DefinitionId = workflowState.DefinitionId,
                Version = workflowState.DefinitionVersion,
                CorrelationId = workflowState.CorrelationId.EmptyIfNull(),
                InstanceId = workflowState.Id
            };
            
            await client.Register(registerRequest, cancellationToken);
        }
        else
        {
            var unregisterRequest = new UnregisterRunningWorkflowRequest
            {
                InstanceId = workflowState.Id
            };
            
            await client.Unregister(unregisterRequest, cancellationToken);
        }
    }
}