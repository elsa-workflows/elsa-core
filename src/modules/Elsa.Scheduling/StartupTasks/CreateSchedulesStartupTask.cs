using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Models;
using Elsa.Scheduling.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Scheduling.StartupTasks;

/// <summary>
/// Enqueues schedule creation when using the default scheduler, which doesn't have its own persistence layer like Quartz or Hangfire.
/// </summary>
[TaskDependency(typeof(PopulateRegistriesStartupTask))]
public class CreateSchedulesStartupTask(IServiceProvider serviceProvider, IOptions<SchedulingOptions> options) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var workQueue = serviceProvider.GetService<ITenantBackgroundWorkQueue>();

        if (workQueue != null)
            await workQueue.EnqueueAsync(CreateSchedulesAsync, cancellationToken);
        else
            await CreateSchedulesAsync(serviceProvider, cancellationToken);
    }

    private async Task CreateSchedulesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();
        var bookmarkStore = serviceProvider.GetRequiredService<IBookmarkStore>();
        var triggerScheduler = serviceProvider.GetRequiredService<ITriggerScheduler>();
        var bookmarkScheduler = serviceProvider.GetRequiredService<IBookmarkScheduler>();
        var pageSize = Math.Max(1, options.Value.StartupSchedulePageSize);
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

        await ScheduleTriggersAsync(triggerStore, triggerScheduler, triggerFilter, pageSize, cancellationToken);
        await ScheduleBookmarksAsync(bookmarkStore, bookmarkScheduler, bookmarkFilter, pageSize, cancellationToken);
    }

    private static async Task ScheduleTriggersAsync(ITriggerStore triggerStore, ITriggerScheduler triggerScheduler, TriggerFilter triggerFilter, int pageSize, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromRange(0, pageSize);

        while (true)
        {
            var page = await triggerStore.FindManyAsync(triggerFilter, pageArgs, cancellationToken);

            if (page.Items.Count == 0)
                break;

            await triggerScheduler.ScheduleAsync(page.Items, cancellationToken);

            var nextOffset = pageArgs.Offset.GetValueOrDefault() + page.Items.Count;
            if (nextOffset >= page.TotalCount)
                break;

            pageArgs = pageArgs.Next();
        }
    }

    private static async Task ScheduleBookmarksAsync(IBookmarkStore bookmarkStore, IBookmarkScheduler bookmarkScheduler, BookmarkFilter bookmarkFilter, int pageSize, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromRange(0, pageSize);

        while (true)
        {
            var page = await bookmarkStore.FindManyAsync(bookmarkFilter, pageArgs, cancellationToken);

            if (page.Items.Count == 0)
                break;

            await bookmarkScheduler.ScheduleAsync(page.Items, cancellationToken);

            var nextOffset = pageArgs.Offset.GetValueOrDefault() + page.Items.Count;
            if (nextOffset >= page.TotalCount)
                break;

            pageArgs = pageArgs.Next();
        }
    }
}
