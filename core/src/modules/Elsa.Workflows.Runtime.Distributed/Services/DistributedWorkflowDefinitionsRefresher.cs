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
        var isRefreshingAll = request.DefinitionIds == null || request.DefinitionIds.Count == 0;
        var lockKey = isRefreshingAll
            ? "WorkflowDefinitionsRefresher:All"
            : $"WorkflowDefinitionsRefresher:{string.Join(",", request.DefinitionIds!.OrderBy(x => x))}";

        await using var distributedLock = await distributedLockProvider.TryAcquireLockAsync(
            lockKey,
            TimeSpan.Zero,
            cancellationToken);

        if (distributedLock == null)
        {
            var logMessage = isRefreshingAll
                ? "Could not acquire lock for refreshing all workflow definitions. Another instance is already refreshing all workflow definitions"
                : "Could not acquire lock for refreshing workflow definitions. Another instance is already refreshing these workflow definitions";

            logger.LogInformation(logMessage);

            var failedDefinitionIds = isRefreshingAll ? Array.Empty<string>() : request.DefinitionIds!;
            return new(Array.Empty<string>(), failedDefinitionIds, RefreshWorkflowDefinitionsStatus.AlreadyInProgress);
        }

        return await inner.RefreshWorkflowDefinitionsAsync(request, cancellationToken);
    }
}