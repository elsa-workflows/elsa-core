using Elsa.Common;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// Periodically purges the bookmark queue of old items.
[UsedImplicitly]
public class PurgeBookmarkQueueRecurringTask(IBookmarkQueuePurger bookmarkQueueWorker) : RecurringTask
{
    public override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return bookmarkQueueWorker.PurgeAsync(stoppingToken);
    }
}