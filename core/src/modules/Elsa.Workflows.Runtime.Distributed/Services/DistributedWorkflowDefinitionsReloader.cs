using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Decorator class that adds distributed locking to the Workflow Definitions Reloader.
/// </summary>
[UsedImplicitly]
public class DistributedWorkflowDefinitionsReloader(
    IWorkflowDefinitionsReloader inner,
    IDistributedLockProvider distributedLockProvider,
    ILogger<DistributedWorkflowDefinitionsReloader> logger) : IWorkflowDefinitionsReloader
{
    private const string LockKey = "WorkflowDefinitionsReloader";
    
    /// <summary>
    /// This ensures that only one instance of the application can reload workflow definitions at a time, preventing potential conflicts and ensuring consistency across distributed environments.
    /// </summary>
    public async Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        await using var distributedLock = await distributedLockProvider.TryAcquireLockAsync(
            LockKey,
            TimeSpan.Zero,
            cancellationToken);
        
        if (distributedLock == null)
        {
            logger.LogInformation("Could not acquire lock for workflow definitions reload. Another instance is already reloading workflow definitions");
            return;
        }
        
        await inner.ReloadWorkflowDefinitionsAsync(cancellationToken);
    }
}