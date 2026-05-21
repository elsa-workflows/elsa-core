using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Stores workflow dispatch commands for at-least-once delivery after their owner workflow state commits.
/// </summary>
public interface IWorkflowDispatchOutbox
{
    /// <summary>
    /// Adds the specified dispatch item to the durable outbox and marks the current workflow state as owning it.
    /// </summary>
    Task EnqueueAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default);
}
