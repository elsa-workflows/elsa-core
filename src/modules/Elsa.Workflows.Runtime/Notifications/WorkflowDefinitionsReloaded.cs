using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// Published when workflow definitions have been reloaded.
public record WorkflowDefinitionsReloaded(ICollection<string> WorkflowDefinitionIds) : INotification;
