using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Periodically purges the bookmark queue of old items.
/// </summary>
/// <remarks>
/// Runs once per cycle for all tenants; items with an empty, null or "*" tenant id are purged under the default tenant.
/// A tenant whose items live in its own database (a connection string in its configuration) is purged separately in its own scope.
/// </remarks>
[RunOnceForAllTenants]
[UsedImplicitly]
public class PurgeBookmarkQueueRecurringTask(IBookmarkQueuePurger bookmarkQueueWorker) : RecurringTask
{
    public override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return bookmarkQueueWorker.PurgeAsync(stoppingToken);
    }
}
