using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A notification that is published when a workflow instance has been dispatched.
/// </summary>
public record WorkflowInstanceDispatched(DispatchWorkflowInstanceRequest Request, DispatchWorkflowResponse Response) : INotification;
