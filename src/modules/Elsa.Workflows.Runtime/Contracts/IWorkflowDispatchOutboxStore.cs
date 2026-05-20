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
    /// Deletes the specified outbox item.
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
