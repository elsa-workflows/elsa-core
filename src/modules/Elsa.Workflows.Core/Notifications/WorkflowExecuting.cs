using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A domain event that is published before a burst of execution begins.  
/// </summary>
public record WorkflowExecuting(Workflow Workflow, WorkflowExecutionContext WorkflowExecutionContext) : INotification;