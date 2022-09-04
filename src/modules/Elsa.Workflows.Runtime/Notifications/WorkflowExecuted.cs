using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowExecuted(Workflow Workflow, WorkflowState WorkflowState) : INotification;