using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Activities.Scheduling.Jobs;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Schedules;

namespace Elsa.Activities.Scheduling.Services;

public class WorkflowBookmarkScheduler : IWorkflowBookmarkScheduler
{
    private const string RootGroupKey = "WorkflowInstance";
    private readonly IJobScheduler _jobScheduler;

    public WorkflowBookmarkScheduler(IJobScheduler jobScheduler)
    {
        _jobScheduler = jobScheduler;
    }

    public async Task ScheduleBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();

        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();

        // Unschedule all triggers for the distinct set of affected workflows.
        var groupKeys = new[] { RootGroupKey, workflowInstanceId };
        await _jobScheduler.ClearAsync(groupKeys, cancellationToken);

        // Schedule a trigger for each bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = JsonSerializer.Deserialize<DelayPayload>(bookmark.Payload!)!;
            var resumeAt = payload.ResumeAt;
            await _jobScheduler.ScheduleAsync(new ResumeWorkflowJob(workflowInstanceId, bookmark), new SpecificInstantSchedule(resumeAt), groupKeys, cancellationToken);
        }
    }
}