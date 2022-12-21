using Elsa.Mediator.Services;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.Protos;
using Elsa.Workflows.Runtime.Notifications;
using Proto.Cluster;
using WorkflowStatus = Elsa.Workflows.Core.Models.WorkflowStatus;

namespace Elsa.ProtoActor.Handlers;

/// <summary>
/// Updates the <see cref="RunningWorkflowsGrain"/> with running workflow instances.
/// </summary>
internal class UpdateRunningWorkflowsHandler : INotificationHandler<WorkflowExecuted>
{
    private readonly Cluster _cluster;

    public UpdateRunningWorkflowsHandler(Cluster cluster)
    {
        _cluster = cluster;
    }

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var client = _cluster.GetRunningWorkflowsGrain();
        var workflowState = notification.WorkflowState;

        if (workflowState.Status == WorkflowStatus.Running)
        {
            var registerRequest = new RegisterRunningWorkflowRequest
            {
                DefinitionId = workflowState.DefinitionId,
                Version = workflowState.DefinitionVersion,
                CorrelationId = workflowState.CorrelationId,
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