using Elsa.Models;
using MediatR;

namespace Elsa.Events;

public record WorkflowStatusChanged(WorkflowInstance WorkflowInstance, WorkflowStatus Status, WorkflowStatus PreviousStatus) : INotification;