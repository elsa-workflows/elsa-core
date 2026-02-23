using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Decorator class that adds distributed locking to the Workflow Definitions Refresher.
/// </summary>
[UsedImplicitly]
public class DistributedWorkflowDefinitionsRefresher(IWorkflowDefinitionsRefresher inner,
    IDistributedLockProvider distributedLockProvider,
    ILogger<DistributedWorkflowDefinitionsRefresher> logger) : IWorkflowDefinitionsRefresher
{
    
    
    /// <summary>
    /// This ensures that only one instance of the application can refresh a set of workflow definitions at a time, preventing potential conflicts and ensuring consistency across distributed environments.
    /// </summary>
    public async Task<RefreshWorkflowDefinitionsResponse> RefreshWorkflowDefinitionsAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DefinitionIds == null 
            || request.DefinitionIds.Count < 1)
        {
            const string refreshAllLockKey = "WorkflowDefinitionsRefresher:All";
            await using var distributedAllLock = await distributedLockProvider.TryAcquireLockAsync(
                refreshAllLockKey,
                TimeSpan.Zero,
                cancellationToken);
            
            if (distributedAllLock == null)
            {
                logger.LogInformation("Could not acquire lock for refreshing all workflow definitions. Another instance is already refreshing all workflow definitions");
                return new RefreshWorkflowDefinitionsResponse(Array.Empty<string>(), Array.Empty<string>(), RefreshWorkflowDefinitionsStatus.AlreadyInProgress);
            }
        }
        else
        {
            var definitionIdsKey = string.Join(",", request.DefinitionIds!.OrderBy(x => x));
            var lockKey = $"WorkflowDefinitionsRefresher:{definitionIdsKey}";
            await using var distributedLock = await distributedLockProvider.TryAcquireLockAsync(
                lockKey,
                TimeSpan.Zero,
                cancellationToken);
            
            if (distributedLock == null)
            {
                logger.LogInformation("Could not acquire lock for refreshing workflow definitions. Another instance is already refreshing these workflow definitions");
                return new RefreshWorkflowDefinitionsResponse(Array.Empty<string>(), request.DefinitionIds!, RefreshWorkflowDefinitionsStatus.AlreadyInProgress);
            }
        }
        
        return await inner.RefreshWorkflowDefinitionsAsync(request, cancellationToken);`
    }
}