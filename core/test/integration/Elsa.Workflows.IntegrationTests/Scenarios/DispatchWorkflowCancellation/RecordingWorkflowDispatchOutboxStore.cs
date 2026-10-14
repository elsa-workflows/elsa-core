using System.Collections.Concurrent;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>
/// Records the child instance ID of every workflow definition dispatch written to the outbox, then delegates. This shows
/// which children a workflow dispatched, even after their pending dispatches have left the outbox again.
/// </summary>
public class RecordingWorkflowDispatchOutboxStore(IWorkflowDispatchOutboxStore inner, ConcurrentQueue<string> enqueuedInstanceIds) : IWorkflowDispatchOutboxStore
{
    public Task SaveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default)
    {
        if (item.WorkflowDefinitionCommand?.InstanceId is { } instanceId)
            enqueuedInstanceIds.Enqueue(instanceId);

        return inner.SaveAsync(item, cancellationToken);
    }

    public Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(CancellationToken cancellationToken = default) => inner.FindManyAsync(cancellationToken);

    public Task<IEnumerable<WorkflowDispatchOutboxItem>> FindManyAsync(int maxCount, CancellationToken cancellationToken = default) => inner.FindManyAsync(maxCount, cancellationToken);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => inner.DeleteAsync(id, cancellationToken);
}
