using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A notification that is published when a workflow instance is being dispatched.
/// </summary>
public record WorkflowInstanceDispatching(DispatchWorkflowInstanceRequest Request) : INotification;
