using Elsa.Common;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Scheduling.HostedServices;

/// <summary>
/// Creates new schedules when using the default scheduler (which doesn't have its own persistence layer like Quartz or Hangfire).
/// </summary>
public class CreateSchedulesBackgroundTask(ITriggerStore triggerStore, IBookmarkStore bookmarkStore, ITriggerScheduler triggerScheduler, IBookmarkScheduler bookmarkScheduler) : BackgroundTask
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var activityTypes = new[] { typeof(Cron), typeof(Timer), typeof(StartAt), typeof(Delay) };
        var activityTypeNames = activityTypes.Select(ActivityTypeNameHelper.GenerateTypeName).ToList();
        var stimulusNames = new[]
        {
            SchedulingStimulusNames.Cron,
            SchedulingStimulusNames.Timer,
            SchedulingStimulusNames.StartAt,
            SchedulingStimulusNames.Delay,
        }.Concat(activityTypeNames).ToList(); // For backwards compatibility with existing bookmark records.
        var triggerFilter = new TriggerFilter { Names = stimulusNames };
        var bookmarkFilter = new BookmarkFilter { Names = stimulusNames };
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, stoppingToken)).ToList();
        var bookmarks = (await bookmarkStore.FindManyAsync(bookmarkFilter, stoppingToken)).ToList();

        await triggerScheduler.ScheduleAsync(triggers, stoppingToken);
        await bookmarkScheduler.ScheduleAsync(bookmarks, stoppingToken);
    }
}