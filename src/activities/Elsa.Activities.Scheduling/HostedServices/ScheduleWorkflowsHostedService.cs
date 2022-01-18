using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Requests;
using Microsoft.Extensions.Hosting;
using IWorkflowTriggerScheduler = Elsa.Activities.Scheduling.Contracts.IWorkflowTriggerScheduler;

namespace Elsa.Activities.Scheduling.HostedServices;

/// <summary>
/// Loads all timer-specific workflow triggers from the database and create scheduled jobs for them. 
/// </summary>
public class ScheduleWorkflowsHostedService : BackgroundService
{
    private readonly IRequestSender _requestSender;
    private readonly IWorkflowTriggerScheduler _workflowTriggerScheduler;
    private readonly IWorkflowBookmarkScheduler _workflowBookmarkScheduler;

    public ScheduleWorkflowsHostedService(
        IRequestSender requestSender,
        IWorkflowTriggerScheduler workflowTriggerScheduler,
        IWorkflowBookmarkScheduler workflowBookmarkScheduler)
    {
        _requestSender = requestSender;
        _workflowTriggerScheduler = workflowTriggerScheduler;
        _workflowBookmarkScheduler = workflowBookmarkScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ScheduleTriggersAsync(stoppingToken);
        await ScheduleBookmarksAsync(stoppingToken);
    }

    private async Task ScheduleTriggersAsync(CancellationToken cancellationToken)
    {
        var timerTriggers = (await _requestSender.RequestAsync(FindWorkflowTriggers.ForTrigger<Timer>(), cancellationToken)).ToImmutableList();
        await _workflowTriggerScheduler.ScheduleTriggersAsync(timerTriggers, cancellationToken);
    }

    private async Task ScheduleBookmarksAsync(CancellationToken cancellationToken)
    {
        var workflowBookmarks = (await _requestSender.RequestAsync(FindWorkflowBookmarks.ForActivity<Delay>(), cancellationToken)).ToImmutableList();
        var groupedBookmarks = workflowBookmarks.GroupBy(x => x.WorkflowInstanceId);

        foreach (var bookmarksGroup in groupedBookmarks)
        {
            var workflowInstanceId = bookmarksGroup.Key;
            var bookmarks = bookmarksGroup.Select(x => x.ToBookmark()).ToList();
            await _workflowBookmarkScheduler.ScheduleBookmarksAsync(workflowInstanceId, bookmarks, cancellationToken);
        }
    }
}