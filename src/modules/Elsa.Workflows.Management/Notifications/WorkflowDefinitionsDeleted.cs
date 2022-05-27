using Elsa.Mediator.Services;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionsDeleted(ICollection<string> DefinitionIds) : INotification;