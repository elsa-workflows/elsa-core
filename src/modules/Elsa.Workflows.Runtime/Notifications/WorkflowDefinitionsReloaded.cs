using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// Published when workflow definitions have been reloaded.
public record WorkflowDefinitionsReloaded(ICollection<ReloadedWorkflowDefinition> ReloadedWorkflowDefinitions) : INotification;