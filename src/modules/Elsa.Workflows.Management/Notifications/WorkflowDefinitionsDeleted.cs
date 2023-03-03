using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionsDeleted(ICollection<string> DefinitionIds) : INotification;