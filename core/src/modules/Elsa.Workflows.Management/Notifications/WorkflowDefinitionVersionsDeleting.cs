using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when specific workflow definition versions are about to be deleted.
/// </summary>
/// <param name="Ids">The IDs of the workflow definitions.</param>
[PublicAPI]
public record WorkflowDefinitionVersionsDeleting(ICollection<string> Ids) : INotification
{
    /// <summary>Requires trigger cleanup to finish before the workflow versions are removed.</summary>
    public bool RequireCompleteTriggerCleanup { get; init; }
}
