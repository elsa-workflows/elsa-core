using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionRetracted(WorkflowDefinition WorkflowDefinition) : INotification;