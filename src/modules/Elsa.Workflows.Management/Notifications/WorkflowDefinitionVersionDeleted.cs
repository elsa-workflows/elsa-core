using Elsa.Mediator.Services;

namespace Elsa.Workflows.Management.Notifications;

public record WorkflowDefinitionVersionDeleted(string DefinitionId, int Version) : INotification;