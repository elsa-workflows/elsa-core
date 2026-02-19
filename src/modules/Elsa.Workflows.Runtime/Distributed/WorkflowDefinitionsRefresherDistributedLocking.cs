using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Decorator class that adds distributed locking to the Workflow Definitions Refresher.
/// </summary>
/// <param name="inner"></param>
/// <param name="distributedLockProvider"></param>
[UsedImplicitly]
public class DistributedWorkflowDefinitionsRefresher(IWorkflowDefinitionsRefresher inner,
    IDistributedLockProvider distributedLockProvider,
    ILogger<WorkflowDefinitionsRefresherDistributedLocking> logger) : IWorkflowDefinitionsRefresher
{
    /// <summary>
    /// This ensures that only one instance of the application can refresh a set of workflow definitions at a time, preventing potential conflicts and ensuring consistency across distributed environments.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<RefreshWorkflowDefinitionsResponse> RefreshWorkflowDefinitionsAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DefinitionIds == null 
            || request.DefinitionIds.Count < 1)
        {
            return new RefreshWorkflowDefinitionsResponse(Array.Empty<string>(), Array.Empty<string>());
        }
        var definitionIdsKey = string.Join(",", request.DefinitionIds!.OrderBy(x => x));
        var lockKey = $"WorkflowDefinitionsRefresher:{definitionIdsKey}";
        await using var distributedLock = await distributedLockProvider.TryAcquireLockAsync(
            lockKey,
            TimeSpan.FromMinutes(1),
            cancellationToken);
        
        if (distributedLock == null)
        {
            logger.LogInformation("Could not acquire lock for workflow definitions refresh. Another instance is already refreshing these workflow definitions");
            return new RefreshWorkflowDefinitionsResponse(Array.Empty<string>(), request.DefinitionIds!);
        }
        
        return await inner.RefreshWorkflowDefinitionsAsync(request, cancellationToken);
    }
}