using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when workflow instances are deleted.
/// </summary>
/// <param name="Ids">The IDs of the workflow instances.</param>
public record WorkflowInstancesDeleted(ICollection<string> Ids) : INotification;