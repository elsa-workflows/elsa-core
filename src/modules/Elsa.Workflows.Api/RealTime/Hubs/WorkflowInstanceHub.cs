using Elsa.Workflows.Api.RealTime.Contracts;
using Elsa.Workflows.Api.RealTime.Messages;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Workflows.Api.RealTime.Hubs;

/// <summary>
/// Represents a SignalR hub for receiving workflow events on the client.
/// </summary>
[PublicAPI]
public class WorkflowInstanceHub : Hub<IWorkflowInstanceClient>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <inheritdoc />
    public WorkflowInstanceHub(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    /// <summary>
    /// Observes a workflow instance.
    /// </summary>
    /// <param name="instanceId">The ID of the workflow instance to observe.</param>
    public async Task ObserveInstanceAsync(string instanceId)
    {
        // Join the user to the workflow instance group.
        await Groups.AddToGroupAsync(Context.ConnectionId, instanceId);
    }
}