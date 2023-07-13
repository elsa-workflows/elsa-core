using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A domain event that is published everytime a burst of execution completes.  
/// </summary>
public record WorkflowExecuted(Workflow Workflow, WorkflowState WorkflowState, WorkflowExecutionContext WorkflowExecutionContext) : INotification;