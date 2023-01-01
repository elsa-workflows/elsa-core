using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Notifications;

public record WorkflowExecuting(Workflow Workflow) : INotification;