using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A domain event that is published before a burst of execution begins.  
/// </summary>
public record WorkflowExecuting(Workflow Workflow) : INotification;