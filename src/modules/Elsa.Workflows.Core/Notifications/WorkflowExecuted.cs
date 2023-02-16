using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A domain event that is published everytime a burst of execution completes.  
/// </summary>
public record WorkflowExecuted(Workflow Workflow, WorkflowState WorkflowState) : INotification;