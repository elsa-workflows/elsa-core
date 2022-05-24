using Elsa.Mediator.Services;
using Elsa.Models;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowExecuting(WorkflowExecutionContext WorkflowExecutionContext) : INotification;