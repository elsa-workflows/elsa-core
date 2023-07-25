using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// An event that is published when workflow execution log records are persisted.
/// </summary>
/// <param name="WorkflowExecutionContext">The workflow execution context.</param>
public record WorkflowExecutionLogUpdated(WorkflowExecutionContext WorkflowExecutionContext) : INotification;