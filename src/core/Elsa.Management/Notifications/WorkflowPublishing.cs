using Elsa.Mediator.Contracts;
using Elsa.Models;

namespace Elsa.Management.Notifications;

public record WorkflowPublishing(Workflow Workflow) : INotification;