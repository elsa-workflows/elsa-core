using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A domain event that is published when a workflow finishes.
/// </summary>
public record WorkflowFinished(Workflow Workflow, WorkflowState WorkflowState, WorkflowExecutionContext WorkflowExecutionContext) : INotification;