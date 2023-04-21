using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Scheduling.Services;

/// <summary>
/// A default implementation of <see cref="ITriggerScheduler"/> that schedules bookmarks using <see cref="IWorkflowScheduler"/>.
/// </summary>
public class DefaultBookmarkScheduler : IBookmarkScheduler
{
    private readonly IWorkflowScheduler _workflowScheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBookmarkScheduler"/> class.
    /// </summary>
    public DefaultBookmarkScheduler(IWorkflowScheduler workflowScheduler)
    {
        _workflowScheduler = workflowScheduler;
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();

        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();

        // Select all StartAt bookmarks.
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();

        // Select all Timer bookmarks.
        var timerBookmarks = bookmarkList.Filter<Activities.Timer>().ToList();

        // Select all Cron bookmarks.
        var cronBookmarks = bookmarkList.Filter<Cron>().ToList();

        // Schedule each Delay bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = bookmark.GetPayload<DelayPayload>();
            var resumeAt = payload.ResumeAt;
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.BookmarkId };
            await _workflowScheduler.ScheduleAtAsync(bookmark.BookmarkId, request, resumeAt, cancellationToken);
        }

        // Schedule a trigger for each StartAt bookmark.
        foreach (var bookmark in startAtBookmarks)
        {
            var payload = bookmark.GetPayload<StartAtPayload>();
            var executeAt = payload.ExecuteAt;
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.BookmarkId };
            await _workflowScheduler.ScheduleAtAsync(bookmark.BookmarkId, request, executeAt, cancellationToken);
        }

        // Schedule a trigger for each Timer bookmark.
        foreach (var bookmark in timerBookmarks)
        {
            var payload = bookmark.GetPayload<TimerBookmarkPayload>();
            var resumeAt = payload.ResumeAt;
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.BookmarkId };
            await _workflowScheduler.ScheduleAtAsync(bookmark.BookmarkId, request, resumeAt, cancellationToken);
        }

        // Schedule a trigger for each Cron bookmark.
        foreach (var bookmark in cronBookmarks)
        {
            var payload = bookmark.GetPayload<CronBookmarkPayload>();
            var cronExpression = payload.CronExpression;
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.BookmarkId };
            await _workflowScheduler.ScheduleCronAsync(bookmark.BookmarkId, request, cronExpression, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();

        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();

        // Select all StartAt bookmarks.
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();

        // Select all Timer bookmarks.
        var timerBookmarks = bookmarkList.Filter<Activities.Timer>().ToList();

        // Select all Cron bookmarks.
        var cronBookmarks = bookmarkList.Filter<Cron>().ToList();

        // Schedule each Delay bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = bookmark.GetPayload<DelayPayload>();
            var resumeAt = payload.ResumeAt;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, resumeAt, cancellationToken);
        }

        // Schedule a trigger for each StartAt bookmark.
        foreach (var bookmark in startAtBookmarks)
        {
            var payload = bookmark.GetPayload<StartAtPayload>();
            var executeAt = payload.ExecuteAt;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, executeAt, cancellationToken);
        }

        // Schedule a trigger for each Timer bookmark.
        foreach (var bookmark in timerBookmarks)
        {
            var payload = bookmark.GetPayload<TimerBookmarkPayload>();
            var resumeAt = payload.ResumeAt;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, resumeAt, cancellationToken);
        }

        // Schedule a trigger for each Cron bookmark.
        foreach (var bookmark in cronBookmarks)
        {
            var payload = bookmark.GetPayload<CronBookmarkPayload>();
            var cronExpression = payload.CronExpression;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            await _workflowScheduler.ScheduleCronAsync(bookmark.Id, request, cronExpression, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();

        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();

        // Select all StartAt bookmarks.
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();

        // Select all Timer bookmarks.
        var timerBookmarks = bookmarkList.Filter<Activities.Timer>().ToList();

        // Select all Cron bookmarks.
        var cronBookmarks = bookmarkList.Filter<Cron>().ToList();

        // Concatenate the filtered bookmarks.
        var bookmarksToUnSchedule = delayBookmarks.Concat(startAtBookmarks).Concat(timerBookmarks).Concat(cronBookmarks).ToList();

        // Unschedule each bookmark.
        foreach (var bookmark in bookmarksToUnSchedule)
            await _workflowScheduler.UnscheduleAsync(bookmark.Id, cancellationToken);
    }
}