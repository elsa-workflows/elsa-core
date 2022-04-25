using Elsa.Mediator.Services;
using Elsa.Models;

namespace Elsa.Management.Notifications;

public record WorkflowRetracting(Workflow Workflow) : INotification;