using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Scheduling.HostedServices;

/// <summary>
/// Creates new schedules when using the default scheduler (which doesn't have its own persistence layer like Quartz or Hangfire).
/// </summary>
public class CreateSchedulesHostedService : BackgroundService
{
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly ITriggerScheduler _triggerScheduler;
    private readonly IBookmarkScheduler _bookmarkScheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSchedulesHostedService"/> class.
    /// </summary>
    public CreateSchedulesHostedService(
        ITriggerStore triggerStore,
        IBookmarkStore bookmarkStore,
        ITriggerScheduler triggerScheduler,
        IBookmarkScheduler bookmarkScheduler)
    {
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _triggerScheduler = triggerScheduler;
        _bookmarkScheduler = bookmarkScheduler;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var activityTypes = new[] { typeof(Cron), typeof(Timer), typeof(StartAt), typeof(Delay) };
        var activityTypeNames = activityTypes.Select(ActivityTypeNameHelper.GenerateTypeName).ToList();
        var triggerFilter = new TriggerFilter { Names = activityTypeNames };
        var bookmarkFilter = new BookmarkFilter { ActivityTypeNames = activityTypeNames };
        var triggers = (await _triggerStore.FindManyAsync(triggerFilter, stoppingToken)).ToList();
        var bookmarks = (await _bookmarkStore.FindManyAsync(bookmarkFilter, stoppingToken)).ToList();

        await _triggerScheduler.ScheduleAsync(triggers, stoppingToken);
        await _bookmarkScheduler.ScheduleAsync(bookmarks, stoppingToken);
    }
}