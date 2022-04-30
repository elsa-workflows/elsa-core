using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Management.Notifications;

public record WorkflowDefinitionPublishing(WorkflowDefinition WorkflowDefinition) : INotification;