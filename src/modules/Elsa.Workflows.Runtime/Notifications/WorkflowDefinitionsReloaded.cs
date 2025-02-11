using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published when workflow definitions have been reloaded.
/// </summary>
public record WorkflowDefinitionsReloaded(ICollection<ReloadedWorkflowDefinition> ReloadedWorkflowDefinitions) : INotification;