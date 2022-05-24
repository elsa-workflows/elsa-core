using Elsa.Mediator.Services;
using Elsa.Models;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowExecuted(WorkflowExecutionContext WorkflowExecutionContext) : INotification;