using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A domain event that is published before a burst of execution begins.  
/// </summary>
public record WorkflowExecuting(Workflow Workflow, WorkflowExecutionContext WorkflowExecutionContext) : INotification;