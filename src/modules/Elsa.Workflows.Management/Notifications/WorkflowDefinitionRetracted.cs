using Elsa.Mediator.Services;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionRetracted(WorkflowDefinition WorkflowDefinition) : INotification;