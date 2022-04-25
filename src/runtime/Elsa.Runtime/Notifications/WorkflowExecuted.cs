using Elsa.Mediator.Services;
using Elsa.Models;

namespace Elsa.Runtime.Notifications;

public record WorkflowExecuted(WorkflowExecutionContext WorkflowExecutionContext) : INotification;