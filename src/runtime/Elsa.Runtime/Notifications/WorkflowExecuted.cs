using Elsa.Mediator.Contracts;
using Elsa.Models;

namespace Elsa.Runtime.Notifications;

public record WorkflowExecuted(WorkflowExecutionContext WorkflowExecutionContext) : INotification;