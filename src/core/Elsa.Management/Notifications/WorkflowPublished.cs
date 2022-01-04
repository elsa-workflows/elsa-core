using Elsa.Mediator.Contracts;
using Elsa.Models;

namespace Elsa.Management.Notifications;
public record WorkflowPublished(Workflow Workflow) : INotification;