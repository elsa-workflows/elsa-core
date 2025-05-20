using Elsa.Common;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Scheduling.HostedServices;

/// <summary>
/// Creates new schedules when using the default scheduler (which doesn't have its own persistence layer like Quartz or Hangfire).
/// </summary>
[UsedImplicitly]
public class CreateSchedulesBackgroundTask(ITriggerStore triggerStore, IBookmarkStore bookmarkStore, ITriggerScheduler triggerScheduler, IBookmarkScheduler bookmarkScheduler) : BackgroundTask
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, stoppingToken)).ToList();
        var bookmarks = (await bookmarkStore.FindManyAsync(bookmarkFilter, stoppingToken)).ToList();

        await triggerScheduler.ScheduleAsync(triggers, stoppingToken);
        await bookmarkScheduler.ScheduleAsync(bookmarks, stoppingToken);
    }
}