using Elsa.Common.Helpers;
using Microsoft.Extensions.Logging;

namespace Elsa.Common.Multitenancy;

public class TenantBackgroundWorkQueueWorker(
    ITenantBackgroundWorkQueue workQueue,
    IServiceProvider serviceProvider,
    ILogger<TenantBackgroundWorkQueueWorker> logger) : BackgroundTask
{
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var enumerator = workQueue.DequeueAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
            while (await enumerator.MoveNextAsync())
            {
                try
                {
                    await enumerator.Current(serviceProvider, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e) when (!e.IsFatal())
                {
                    logger.LogError(e, "A tenant background work item failed.");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Tenant background work queue worker stopped because cancellation was requested.");
        }
    }
}
