using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Durable store for workflow dispatch outbox items.
/// </summary>
public interface IWorkflowDispatchOutboxStore
{
    /// <summary>
    /// Saves the specified outbox item.
    /// </summary>
    Task SaveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all pending outbox items.
    /// </summary>
    Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to the specified number of pending outbox items.
    /// </summary>
    /// <param name="maxCount">The maximum number of pending items to return. Values less than or equal to 0 disable the limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(int maxCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified outbox item.
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
