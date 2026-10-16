using System.Collections.Concurrent;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>
/// The child instance ID and correlation ID of a workflow definition dispatch written to the outbox.
/// </summary>
public sealed record EnqueuedChildDispatch(string InstanceId, string? CorrelationId);

/// <summary>
/// Records every workflow definition dispatch written to the outbox, then delegates. This shows which children a
/// workflow dispatched, even after their pending dispatches have left the outbox again. Deletes fail while
/// <paramref name="failDeletes"/> returns true.
/// </summary>
public class RecordingWorkflowDispatchOutboxStore(IWorkflowDispatchOutboxStore inner, ConcurrentQueue<EnqueuedChildDispatch> enqueuedDispatches, Func<bool> failDeletes) : IWorkflowDispatchOutboxStore
{
    public Task SaveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default)
    {
        if (item.WorkflowDefinitionCommand?.InstanceId is { } instanceId)
            enqueuedDispatches.Enqueue(new EnqueuedChildDispatch(instanceId, item.WorkflowDefinitionCommand.CorrelationId));

        return inner.SaveAsync(item, cancellationToken);
    }

    public Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(CancellationToken cancellationToken = default) => inner.FindManyAsync(cancellationToken);

    public Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(int maxCount, CancellationToken cancellationToken = default) => inner.FindManyAsync(maxCount, cancellationToken);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        failDeletes() ? throw new InvalidOperationException("Simulated outbox store failure.") : inner.DeleteAsync(id, cancellationToken);
}
