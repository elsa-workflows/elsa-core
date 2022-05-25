using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionPublishing(WorkflowDefinition WorkflowDefinition) : INotification;