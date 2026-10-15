using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A notification that is published when a workflow definition has been dispatched.
/// </summary>
public record WorkflowDefinitionDispatched(DispatchWorkflowDefinitionRequest Request, DispatchWorkflowResponse Response) : INotification;
