using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A domain event that is published when a workflow starts.
/// </summary>
public record WorkflowStarted(Workflow Workflow, WorkflowExecutionContext WorkflowExecutionContext) : INotification;