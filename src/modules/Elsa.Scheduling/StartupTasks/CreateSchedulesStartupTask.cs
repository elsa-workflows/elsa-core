using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.StartupTasks;

/// <summary>
/// Enqueues schedule creation when using the default scheduler, which doesn't have its own persistence layer like Quartz or Hangfire.
/// </summary>
public class CreateSchedulesStartupTask(ITenantBackgroundWorkQueue workQueue) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await workQueue.EnqueueAsync(CreateSchedulesAsync, cancellationToken);
    }

    private static async Task CreateSchedulesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();
        var bookmarkStore = serviceProvider.GetRequiredService<IBookmarkStore>();
        var triggerScheduler = serviceProvider.GetRequiredService<ITriggerScheduler>();
        var bookmarkScheduler = serviceProvider.GetRequiredService<IBookmarkScheduler>();
        var stimulusNames = new[]
        {
            SchedulingStimulusNames.Cron, SchedulingStimulusNames.Timer, SchedulingStimulusNames.StartAt, SchedulingStimulusNames.Delay,
        };
        var triggerFilter = new TriggerFilter
        {
            Names = stimulusNames
        };
        var bookmarkFilter = new BookmarkFilter
        {
            Names = stimulusNames
        };
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();
        var bookmarks = (await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).ToList();

        await triggerScheduler.ScheduleAsync(triggers, cancellationToken);
        await bookmarkScheduler.ScheduleAsync(bookmarks, cancellationToken);
    }
}
