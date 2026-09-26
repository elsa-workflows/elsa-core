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
    private const int MaxConcurrentSchedulingOperations = 16;
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
        var schedules = delayBookmarks
            .Select(bookmark => CreateScheduleAt(bookmark.Id, bookmark.WorkflowInstanceId, bookmark.GetPayload<DelayPayload>().ResumeAt))
            .Concat(startAtBookmarks.Select(bookmark => CreateScheduleAt(bookmark.Id, bookmark.WorkflowInstanceId, bookmark.GetPayload<StartAtPayload>().ExecuteAt)))
            .Concat(timerBookmarks.Select(bookmark => CreateScheduleAt(bookmark.Id, bookmark.WorkflowInstanceId, bookmark.GetPayload<TimerBookmarkPayload>().ResumeAt)))
            .Concat(cronBookmarks.Select(bookmark => CreateCronSchedule(bookmark.Id, bookmark.WorkflowInstanceId, bookmark.GetPayload<CronBookmarkPayload>().CronExpression)));

        await ScheduleAsync(schedules, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        var delayBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Delay);
        var startAtBookmarks = bookmarkList.Filter(SchedulingStimulusNames.StartAt);
        var timerBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Timer);
        var cronBookmarks = bookmarkList.Filter(SchedulingStimulusNames.Cron);
        var schedules = delayBookmarks
            .Select(bookmark => CreateScheduleAt(bookmark.Id, workflowInstanceId, bookmark.GetPayload<DelayPayload>().ResumeAt))
            .Concat(startAtBookmarks.Select(bookmark => CreateScheduleAt(bookmark.Id, workflowInstanceId, bookmark.GetPayload<StartAtPayload>().ExecuteAt)))
            .Concat(timerBookmarks.Select(bookmark => CreateScheduleAt(bookmark.Id, workflowInstanceId, bookmark.GetPayload<TimerBookmarkPayload>().ResumeAt)))
            .Concat(cronBookmarks.Select(bookmark => CreateCronSchedule(bookmark.Id, workflowInstanceId, bookmark.GetPayload<CronBookmarkPayload>().CronExpression)));

        await ScheduleAsync(schedules, cancellationToken);
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

    private Task ScheduleAsync(IEnumerable<BookmarkSchedule> schedules, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrentSchedulingOperations,
            CancellationToken = cancellationToken
        };

        return Parallel.ForEachAsync(schedules, options, ScheduleBookmarkAsync);
    }

    private async ValueTask ScheduleBookmarkAsync(BookmarkSchedule schedule, CancellationToken cancellationToken)
    {
        var request = new ScheduleExistingWorkflowInstanceRequest
        {
            WorkflowInstanceId = schedule.WorkflowInstanceId,
            BookmarkId = schedule.BookmarkId
        };

        if (schedule.CronExpression != null)
            await _workflowScheduler.ScheduleCronAsync(schedule.BookmarkId, request, schedule.CronExpression, cancellationToken);
        else
            await _workflowScheduler.ScheduleAtAsync(schedule.BookmarkId, request, schedule.ExecuteAt, cancellationToken);
    }

    private static BookmarkSchedule CreateScheduleAt(string bookmarkId, string workflowInstanceId, DateTimeOffset executeAt)
    {
        return new BookmarkSchedule(bookmarkId, workflowInstanceId, executeAt, null);
    }

    private static BookmarkSchedule CreateCronSchedule(string bookmarkId, string workflowInstanceId, string cronExpression)
    {
        return new BookmarkSchedule(bookmarkId, workflowInstanceId, default, cronExpression);
    }

    private readonly record struct BookmarkSchedule(string BookmarkId, string WorkflowInstanceId, DateTimeOffset ExecuteAt, string? CronExpression);
}
