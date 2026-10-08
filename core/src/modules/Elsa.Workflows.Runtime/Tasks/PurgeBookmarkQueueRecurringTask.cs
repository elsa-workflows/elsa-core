using Elsa.Common;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Periodically purges the bookmark queue of old items.
/// </summary>
[UsedImplicitly]
public class PurgeBookmarkQueueRecurringTask(IBookmarkQueuePurger bookmarkQueueWorker) : RecurringTask
{
    public override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return bookmarkQueueWorker.PurgeAsync(stoppingToken);
    }
}