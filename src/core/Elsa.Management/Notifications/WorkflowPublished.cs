using Elsa.Mediator.Services;
using Elsa.Models;

namespace Elsa.Management.Notifications;
public record WorkflowPublished(Workflow Workflow) : INotification;