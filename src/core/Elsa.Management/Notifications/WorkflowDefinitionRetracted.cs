using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Management.Notifications;

public record WorkflowDefinitionRetracted(WorkflowDefinition WorkflowDefinition) : INotification;