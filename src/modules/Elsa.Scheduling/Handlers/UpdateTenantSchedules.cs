using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Scheduling.Handlers;

public class UpdateTenantSchedules : ITenantActivatedEvent, ITenantDeletedEvent
{
    private static readonly string[] ActivityTypeNames =
    [
        ActivityTypeNameHelper.GenerateTypeName<Cron>(),
        ActivityTypeNameHelper.GenerateTypeName<Timer>(),
        ActivityTypeNameHelper.GenerateTypeName<Delay>()
    ];

    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        var serviceProvider = args.TenantScope.ServiceProvider;
        var cancellationToken = args.CancellationToken;
        var triggerScheduler = args.TenantScope.ServiceProvider.GetRequiredService<ITriggerScheduler>();
        var bookmarkScheduler = args.TenantScope.ServiceProvider.GetRequiredService<IBookmarkScheduler>();
        var triggers = (await GetTriggersAsync(serviceProvider, cancellationToken)).ToList();
        var bookmarks = (await GetBookmarksAsync(serviceProvider, cancellationToken)).ToList();

        await triggerScheduler.ScheduleAsync(triggers, args.CancellationToken);
        await bookmarkScheduler.ScheduleAsync(bookmarks, args.CancellationToken);
    }

    public async Task TenantDeletedAsync(TenantDeletedEventArgs args)
    {
        var serviceProvider = args.TenantScope.ServiceProvider;
        var cancellationToken = args.CancellationToken;
        var triggerScheduler = args.TenantScope.ServiceProvider.GetRequiredService<ITriggerScheduler>();
        var bookmarkScheduler = args.TenantScope.ServiceProvider.GetRequiredService<IBookmarkScheduler>();
        var triggers = (await GetTriggersAsync(serviceProvider, cancellationToken)).ToList();
        var bookmarks = (await GetBookmarksAsync(serviceProvider, cancellationToken)).ToList();

        await triggerScheduler.UnscheduleAsync(triggers, args.CancellationToken);
        await bookmarkScheduler.UnscheduleAsync(bookmarks, args.CancellationToken);
    }

    private async Task<IEnumerable<StoredTrigger>> GetTriggersAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();
        var filter = TriggerFilter.ByNames(ActivityTypeNames);
        return await triggerStore.FindManyAsync(filter, cancellationToken);
    }

    private async Task<IEnumerable<StoredBookmark>> GetBookmarksAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var bookmarkStore = serviceProvider.GetRequiredService<IBookmarkStore>();
        var filter = BookmarkFilter.ByActivityTypeNames(ActivityTypeNames);
        return await bookmarkStore.FindManyAsync(filter, cancellationToken);
    }
}