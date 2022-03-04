using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Modules.Scheduling.Contracts;
using Elsa.Persistence.Requests;
using Microsoft.Extensions.Hosting;
using IWorkflowTriggerScheduler = Elsa.Modules.Scheduling.Contracts.IWorkflowTriggerScheduler;
using Timer = Elsa.Modules.Scheduling.Triggers.Timer;

namespace Elsa.Modules.Scheduling.HostedServices;

/// <summary>
/// Loads all timer-specific workflow bookmarks from the database and create scheduled jobs for them. 
/// </summary>
public class ScheduleWorkflows : BackgroundService
{
    private readonly IRequestSender _requestSender;
    private readonly IWorkflowBookmarkScheduler _workflowBookmarkScheduler;

    public ScheduleWorkflows(IRequestSender requestSender, IWorkflowBookmarkScheduler workflowBookmarkScheduler)
    {
        _requestSender = requestSender;
        _workflowBookmarkScheduler = workflowBookmarkScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ScheduleBookmarksAsync(stoppingToken);
    }

    private async Task ScheduleBookmarksAsync(CancellationToken cancellationToken)
    {
        var workflowBookmarks = (await _requestSender.RequestAsync(FindWorkflowBookmarks.ForActivity<Delay>(), cancellationToken)).ToImmutableList();
        var groupedBookmarks = workflowBookmarks.GroupBy(x => x.WorkflowInstanceId);

        foreach (var bookmarksGroup in groupedBookmarks)
        {
            var workflowInstanceId = bookmarksGroup.Key;
            await _workflowBookmarkScheduler.ScheduleBookmarksAsync(workflowInstanceId, bookmarksGroup, cancellationToken);
        }
    }
}