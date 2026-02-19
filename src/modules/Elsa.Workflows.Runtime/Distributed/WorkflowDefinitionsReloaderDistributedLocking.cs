using JetBrains.Annotations;
using Medallion.Threading;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Decorator class that adds Distributed Locking to the Workflow Definitions Reloader.
/// </summary>
/// <param name="inner"></param>
/// <param name="distributedLockProvider"></param>
[UsedImplicitly]
public class WorkflowDefinitionsReloaderDistributedLocking(
    IWorkflowDefinitionsReloader inner,
    IDistributedLockProvider distributedLockProvider) : IWorkflowDefinitionsReloader
{
    private const string LockKey = "WorkflowDefinitionsReloader";
    
    /// <summary>
    /// This ensures that only one instance of the application can reload workflow definitions at a time, preventing potential conflicts and ensuring consistency across distributed environments.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        await using var distributedLock = await distributedLockProvider.TryAcquireLockAsync(
            LockKey,
            TimeSpan.FromMinutes(1),
            cancellationToken);
        
        if (distributedLock == null)
        {
            // Another instance is already reloading workflow definitions. Skip this reload.
            return;
        }
        
        await inner.ReloadWorkflowDefinitionsAsync(cancellationToken);
    }
}