using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Persistence.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.Scheduling.HostedServices;

/// <summary>
/// Loads all timer-specific workflow bookmarks from the database and create scheduled jobs for them. 
/// </summary>
public class ScheduleWorkflowsHostedService : BackgroundService
{
    private readonly IWorkflowBookmarkStore _bookmarkStore;
    private readonly IWorkflowBookmarkScheduler _workflowBookmarkScheduler;

    public ScheduleWorkflowsHostedService(IWorkflowBookmarkStore bookmarkStore, IWorkflowBookmarkScheduler workflowBookmarkScheduler)
    {
        _bookmarkStore = bookmarkStore;
        _workflowBookmarkScheduler = workflowBookmarkScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ScheduleBookmarksAsync(stoppingToken);
    }

    private async Task ScheduleBookmarksAsync(CancellationToken cancellationToken)
    {
        var workflowBookmarks = (await _bookmarkStore.FindManyAsync(ActivityTypeNameHelper.GenerateTypeName<Delay>(), cancellationToken: cancellationToken)).ToImmutableList();
        var groupedBookmarks = workflowBookmarks.GroupBy(x => x.WorkflowInstanceId);

        foreach (var bookmarksGroup in groupedBookmarks)
        {
            var workflowInstanceId = bookmarksGroup.Key;
            var bookmarks = bookmarksGroup.Select(x => x.ToBookmark());
            await _workflowBookmarkScheduler.ScheduleBookmarksAsync(workflowInstanceId, bookmarks, cancellationToken);
        }
    }
}