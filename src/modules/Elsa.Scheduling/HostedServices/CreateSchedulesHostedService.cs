using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Scheduling.HostedServices;

/// <summary>
/// Creates new schedules when using the default scheduler (which doesn't have its own persistence layer like Quartz or Hangfire).
/// </summary>
public class CreateSchedulesHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSchedulesHostedService"/> class.
    /// </summary>
    public CreateSchedulesHostedService(IServiceScopeFactory scopeFactory
)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var triggerStore = scope.ServiceProvider.GetRequiredService<ITriggerStore>();
        var bookmarkStore = scope.ServiceProvider.GetRequiredService<IBookmarkStore>();
        var triggerScheduler = scope.ServiceProvider.GetRequiredService<ITriggerScheduler>();
        var bookmarkScheduler = scope.ServiceProvider.GetRequiredService<IBookmarkScheduler>();

        var activityTypes = new[] { typeof(Cron), typeof(Timer), typeof(StartAt), typeof(Delay) };
        var activityTypeNames = activityTypes.Select(ActivityTypeNameHelper.GenerateTypeName).ToList();
        var triggerFilter = new TriggerFilter { Names = activityTypeNames };
        var bookmarkFilter = new BookmarkFilter { ActivityTypeNames = activityTypeNames };
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, stoppingToken)).ToList();
        var bookmarks = (await bookmarkStore.FindManyAsync(bookmarkFilter, stoppingToken)).ToList();

        await triggerScheduler.ScheduleAsync(triggers, stoppingToken);
        await bookmarkScheduler.ScheduleAsync(bookmarks, stoppingToken);
    }
}