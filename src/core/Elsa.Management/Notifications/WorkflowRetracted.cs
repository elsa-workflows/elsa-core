using Elsa.Mediator.Services;
using Elsa.Models;

namespace Elsa.Management.Notifications;

public record WorkflowRetracted(Workflow Workflow) : INotification;