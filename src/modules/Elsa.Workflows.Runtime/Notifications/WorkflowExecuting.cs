using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowExecuting(WorkflowExecutionContext WorkflowExecutionContext) : INotification;