using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Jobs.Schedules;
using Elsa.Jobs.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Jobs;
using Elsa.Scheduling.Services;

namespace Elsa.Scheduling.Implementations;

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
        
        // Select all StartAt bookmarks.
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();
        
        var groupKeys = new[] { RootGroupKey, workflowInstanceId };

        // Schedule a trigger for each Delay bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = JsonSerializer.Deserialize<DelayPayload>(bookmark.Data!)!;
            var resumeAt = payload.ResumeAt;
            var job = new ResumeWorkflowJob(workflowInstanceId, bookmark.ToBookmark());
            var schedule = new SpecificInstantSchedule(resumeAt);
            await _jobScheduler.ScheduleAsync(job, bookmark.Id, schedule, groupKeys, cancellationToken);
        }
        
        // Schedule a trigger for each StartAt bookmark.
        foreach (var bookmark in startAtBookmarks)
        {
            var payload = JsonSerializer.Deserialize<StartAtPayload>(bookmark.Data!)!;
            var executeAt = payload.ExecuteAt;
            var job = new ResumeWorkflowJob(workflowInstanceId, bookmark.ToBookmark());
            var schedule = new SpecificInstantSchedule(executeAt);
            await _jobScheduler.ScheduleAsync(job, bookmark.Id, schedule, groupKeys, cancellationToken);
        }
    }

    public async Task UnscheduleBookmarksAsync(string workflowInstanceId, IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();
        var bookmarksToUnSchedule = delayBookmarks.Concat(startAtBookmarks).ToList();

        foreach (var bookmark in bookmarksToUnSchedule) 
            await _jobScheduler.UnscheduleAsync(bookmark.Id, cancellationToken);
    }
}