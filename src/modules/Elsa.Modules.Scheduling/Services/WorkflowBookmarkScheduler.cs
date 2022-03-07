using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Modules.Scheduling.Contracts;
using Elsa.Modules.Scheduling.Jobs;
using Elsa.Persistence.Entities;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Schedules;
using Elsa.Persistence.Extensions;

namespace Elsa.Modules.Scheduling.Services;

public class WorkflowBookmarkScheduler : IWorkflowBookmarkScheduler
{
    private const string RootGroupKey = "WorkflowInstance";
    private readonly IJobScheduler _jobScheduler;

    public WorkflowBookmarkScheduler(IJobScheduler jobScheduler)
    {
        _jobScheduler = jobScheduler;
    }

    public async Task ScheduleBookmarksAsync(string workflowInstanceId, IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();

        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();
        var groupKeys = new[] { RootGroupKey, workflowInstanceId };

        // Schedule a trigger for each bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = JsonSerializer.Deserialize<DelayPayload>(bookmark.Data!)!;
            var resumeAt = payload.ResumeAt;
            var job = new ResumeWorkflowJob(workflowInstanceId, bookmark.ToBookmark());
            var schedule = new SpecificInstantSchedule(resumeAt);
            await _jobScheduler.ScheduleAsync(job, schedule, groupKeys, cancellationToken);
        }
    }

    public async Task UnscheduleBookmarksAsync(string workflowInstanceId, IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();

        foreach (var bookmark in delayBookmarks)
        {
            var job = new ResumeWorkflowJob(workflowInstanceId, bookmark.ToBookmark());
            await _jobScheduler.UnscheduleAsync(job, cancellationToken);
        }
    }
}