using Elsa.Extensions;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;

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
        var delayBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Delay);
        var startAtBookmarks = bookmarkList.Filter(SchedulingStimulusNames.StartAt);
        var timerBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Timer);
        var cronBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Cron);

        // Schedule each Delay bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = bookmark.GetPayload<DelayPayload>();
            var resumeAt = payload.ResumeAt;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = bookmark.WorkflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, resumeAt, cancellationToken);
        }

        // Schedule each StartAt bookmark.
        foreach (var bookmark in startAtBookmarks)
        {
            var payload = bookmark.GetPayload<StartAtPayload>();
            var executeAt = payload.ExecuteAt;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = bookmark.WorkflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, executeAt, cancellationToken);
        }

        // Schedule each Timer bookmark.
        foreach (var bookmark in timerBookmarks)
        {
            var payload = bookmark.GetPayload<TimerBookmarkPayload>();
            var resumeAt = payload.ResumeAt;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = bookmark.WorkflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, resumeAt, cancellationToken);
        }

        // Schedule each Cron bookmark.
        foreach (var bookmark in cronBookmarks)
        {
            var payload = bookmark.GetPayload<CronBookmarkPayload>();
            var cronExpression = payload.CronExpression;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = bookmark.WorkflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleCronAsync(bookmark.Id, request, cronExpression, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        var delayBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Delay);
        var startAtBookmarks = bookmarkList.Filter(SchedulingStimulusNames.StartAt);
        var timerBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Timer);
        var cronBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Cron);

        // Schedule each Delay bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = bookmark.GetPayload<DelayPayload>();
            var resumeAt = payload.ResumeAt;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = workflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, resumeAt, cancellationToken);
        }

        // Schedule each StartAt bookmark.
        foreach (var bookmark in startAtBookmarks)
        {
            var payload = bookmark.GetPayload<StartAtPayload>();
            var executeAt = payload.ExecuteAt;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = workflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, executeAt, cancellationToken);
        }

        // Schedule each Timer bookmark.
        foreach (var bookmark in timerBookmarks)
        {
            var payload = bookmark.GetPayload<TimerBookmarkPayload>();
            var resumeAt = payload.ResumeAt;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = workflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, resumeAt, cancellationToken);
        }

        // Schedule each Cron bookmark.
        foreach (var bookmark in cronBookmarks)
        {
            var payload = bookmark.GetPayload<CronBookmarkPayload>();
            var cronExpression = payload.CronExpression;
            var request = new ScheduleExistingWorkflowInstanceRequest
            {
                WorkflowInstanceId = workflowInstanceId,
                BookmarkId = bookmark.Id
            };
            await _workflowScheduler.ScheduleCronAsync(bookmark.Id, request, cronExpression, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        var delayBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Delay);
        var startAtBookmarks = bookmarkList.Filter(SchedulingStimulusNames.StartAt);
        var timerBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Timer);
        var cronBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Cron);
        var bookmarksToUnSchedule = delayBookmarks.Concat(startAtBookmarks).Concat(timerBookmarks).Concat(cronBookmarks).ToList();

        foreach (var bookmark in bookmarksToUnSchedule)
            await _workflowScheduler.UnscheduleAsync(bookmark.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        var delayBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Delay);
        var startAtBookmarks = bookmarkList.Filter(SchedulingStimulusNames.StartAt);
        var timerBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Timer);
        var cronBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Cron);
        var bookmarksToUnSchedule = delayBookmarks.Concat(startAtBookmarks).Concat(timerBookmarks).Concat(cronBookmarks).ToList();

        foreach (var bookmark in bookmarksToUnSchedule)
            await _workflowScheduler.UnscheduleAsync(bookmark.Id, cancellationToken);
    }
}