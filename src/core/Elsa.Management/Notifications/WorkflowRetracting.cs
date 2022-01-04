using Elsa.Mediator.Contracts;
using Elsa.Models;

namespace Elsa.Management.Notifications;

public record WorkflowRetracting(Workflow Workflow) : INotification;