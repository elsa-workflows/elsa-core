using System.Text.Json;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Schedules;
using Elsa.Scheduling.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Scheduling.Services;

/// <inheritdoc />
public class BookmarkScheduler : IBookmarkScheduler
{
    private const string RootGroupKey = "WorkflowInstance";
    private readonly IScheduler _scheduler;
    private readonly IWorkflowDispatcher _workflowDispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarkScheduler"/> class.
    /// </summary>
    public BookmarkScheduler(IScheduler scheduler, IWorkflowDispatcher workflowDispatcher)
    {
        _scheduler = scheduler;
        _workflowDispatcher = workflowDispatcher;
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();

        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();

        // Select all StartAt bookmarks.
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();

        var groupKeys = new[] { RootGroupKey, workflowInstanceId };

        // Schedule each Delay bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = JsonSerializer.Deserialize<DelayPayload>(bookmark.Data!)!;
            var resumeAt = payload.ResumeAt;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            var task = new ResumeWorkflowTask(request);
            var schedule = new SpecificInstantSchedule(resumeAt);
            await _scheduler.ScheduleAsync(bookmark.Id, task, schedule, groupKeys, cancellationToken);
        }

        // Schedule a trigger for each StartAt bookmark.
        foreach (var bookmark in startAtBookmarks)
        {
            var payload = JsonSerializer.Deserialize<StartAtPayload>(bookmark.Data!)!;
            var executeAt = payload.ExecuteAt;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            var task = new ResumeWorkflowTask(request);
            var schedule = new SpecificInstantSchedule(executeAt);
            await _scheduler.ScheduleAsync(bookmark.Id, task, schedule, groupKeys, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();
        var bookmarksToUnSchedule = delayBookmarks.Concat(startAtBookmarks).ToList();

        foreach (var bookmark in bookmarksToUnSchedule) 
            await _scheduler.UnscheduleAsync(bookmark.Id, cancellationToken);
    }
}