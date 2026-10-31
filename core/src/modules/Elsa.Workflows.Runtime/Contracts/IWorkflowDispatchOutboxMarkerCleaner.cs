using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Removes a committed outbox marker from its owner workflow after the outbox item has been deleted.
/// </summary>
/// <remarks>
/// Hosts can replace this service when workflow state mutations require additional locking or persistence coordination.
/// Implementations must acquire ownership before reloading and saving the owner, tolerate a missing owner or marker,
/// and leave other workflow state unchanged. The owner is identified by
/// <see cref="WorkflowDispatchOutboxItem.OwnerWorkflowInstanceId"/>, not by the dispatch target.
/// The processor holds its processor lock, but does not acquire a workflow instance lock before calling this service.
/// Calls may occur during the owner's post-commit notification while execution still holds an instance lock.
/// The outbox item is already deleted, including when delivery was abandoned. Cleanup failures are logged without
/// recreating the item or retrying delivery. Implementations that defer cleanup must arrange their own durable reconciliation.
/// </remarks>
public interface IWorkflowDispatchOutboxMarkerCleaner
{
    /// <summary>
    /// Removes the item's committed marker, if the owner and marker still exist.
    /// </summary>
    Task RemoveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default);
}