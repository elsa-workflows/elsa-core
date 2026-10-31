using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when workflow instances are about to be deleted.
/// </summary>
/// <param name="Ids">The IDs of the workflow instances.</param>
public record WorkflowInstancesDeleting(ICollection<string> Ids) : INotification;