using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Models;
using Elsa.Scheduling.Options;
using Elsa.Scheduling.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Scheduling.StartupTasks;

/// <summary>
/// Enqueues schedule creation when using the default scheduler, which doesn't have its own persistence layer like Quartz or Hangfire.
/// Scheduling bookmarks whose workflow instance is missing or finished are skipped and purged so startup does not rehydrate dead work.
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
        var bookmarkReconciler = new SchedulingBookmarkReconciler(
            serviceProvider.GetRequiredService<IWorkflowInstanceStore>(),
            serviceProvider.GetRequiredService<IBookmarkManager>());
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
        await ScheduleBookmarksAsync(bookmarkStore, bookmarkScheduler, bookmarkReconciler, bookmarkFilter, pageSize, cancellationToken);
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

    private static async Task ScheduleBookmarksAsync(
        IBookmarkStore bookmarkStore,
        IBookmarkScheduler bookmarkScheduler,
        SchedulingBookmarkReconciler bookmarkReconciler,
        BookmarkFilter bookmarkFilter,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromRange(0, pageSize);
        var orphanBookmarks = new List<StoredBookmark>();

        while (true)
        {
            var page = await bookmarkStore.FindManyAsync(bookmarkFilter, pageArgs, cancellationToken);

            if (page.Items.Count == 0)
                break;

            var classification = await bookmarkReconciler.ClassifyAsync(page.Items, cancellationToken);

            if (classification.Orphans.Count > 0)
                orphanBookmarks.AddRange(classification.Orphans);

            if (classification.Schedulable.Count > 0)
                await bookmarkScheduler.ScheduleAsync(classification.Schedulable, cancellationToken);

            var nextOffset = pageArgs.Offset.GetValueOrDefault() + page.Items.Count;
            if (nextOffset >= page.TotalCount)
                break;

            pageArgs = pageArgs.Next();
        }

        await bookmarkReconciler.PurgeAsync(orphanBookmarks, cancellationToken);
    }
}
