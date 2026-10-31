using Elsa.Common.DistributedHosting;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Removes committed outbox markers by reloading and saving the owner under its distributed workflow instance lock.
/// </summary>
public class WorkflowDispatchOutboxMarkerCleaner(
    IWorkflowInstanceStore workflowInstanceStore,
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> distributedLockingOptions) : IWorkflowDispatchOutboxMarkerCleaner
{
    /// <inheritdoc />
    public async Task RemoveAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken = default)
    {
        var lockResource = $"workflow-instance:{item.OwnerWorkflowInstanceId}";
        await using var handle = await distributedLockProvider.AcquireLockAsync(lockResource, distributedLockingOptions.Value.LockAcquisitionTimeout, cancellationToken);
        var owner = await workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = item.OwnerWorkflowInstanceId }, cancellationToken);

        if (owner == null)
        {
            return;
        }

        if (!owner.WorkflowState.RemoveWorkflowDispatchOutboxItem(item.Id))
        {
            return;
        }

        await workflowInstanceStore.SaveAsync(owner, cancellationToken);
    }
}