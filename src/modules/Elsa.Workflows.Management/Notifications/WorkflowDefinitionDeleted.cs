using Elsa.Mediator.Services;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionDeleted(string DefinitionId) : INotification;