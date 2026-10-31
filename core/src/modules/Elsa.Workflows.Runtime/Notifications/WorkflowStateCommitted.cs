using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowStateCommitted(WorkflowExecutionContext WorkflowExecutionContext, WorkflowState WorkflowState, WorkflowInstance WorkflowInstance) : INotification;