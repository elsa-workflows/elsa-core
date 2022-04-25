using Elsa.Mediator.Services;
using Elsa.Models;

namespace Elsa.Runtime.Notifications;

public record WorkflowExecuting(WorkflowExecutionContext WorkflowExecutionContext) : INotification;