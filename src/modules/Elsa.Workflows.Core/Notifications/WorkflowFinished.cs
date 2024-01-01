using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A domain event that is published when a workflow finishes.
/// </summary>
public record WorkflowFinished(Workflow Workflow, WorkflowState WorkflowState, WorkflowExecutionContext WorkflowExecutionContext) : INotification;