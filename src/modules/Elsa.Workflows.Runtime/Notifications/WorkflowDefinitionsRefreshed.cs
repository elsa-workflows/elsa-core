using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// Published when workflow definitions have been refreshed.
public record WorkflowDefinitionsRefreshed(ICollection<string> WorkflowDefinitionIds) : INotification;