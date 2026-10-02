using Elsa.Common;
using Elsa.Common.RecurringTasks;
using Elsa.Retention.Jobs;
using JetBrains.Annotations;

namespace Elsa.Retention;

/// <summary>
/// Periodically deletes workflow instances and their execution logs.
/// </summary>
[SingleNodeTask]
[UsedImplicitly]
public class CleanupRecurringTask(CleanupJob job) : RecurringTask
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await job.ExecuteAsync(stoppingToken);
    }
}