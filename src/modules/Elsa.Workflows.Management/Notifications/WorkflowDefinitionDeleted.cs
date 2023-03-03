using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionDeleted(string DefinitionId) : INotification;