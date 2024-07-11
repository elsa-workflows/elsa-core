using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A domain event that is published everytime a burst of execution completes.  
/// </summary>
public record WorkflowExecuted(Workflow Workflow, WorkflowState WorkflowState, WorkflowExecutionContext WorkflowExecutionContext) : INotification;