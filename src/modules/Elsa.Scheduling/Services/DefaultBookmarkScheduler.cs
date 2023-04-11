using System.Text.Json;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Core.Models;
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
    public async Task ScheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();

        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();

        // Select all StartAt bookmarks.
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();

        // Schedule each Delay bookmark.
        foreach (var bookmark in delayBookmarks)
        {
            var payload = JsonSerializer.Deserialize<DelayPayload>(bookmark.Data!)!;
            var resumeAt = payload.ResumeAt;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, resumeAt, cancellationToken);
        }

        // Schedule a trigger for each StartAt bookmark.
        foreach (var bookmark in startAtBookmarks)
        {
            var payload = JsonSerializer.Deserialize<StartAtPayload>(bookmark.Data!)!;
            var executeAt = payload.ExecuteAt;
            var request = new DispatchWorkflowInstanceRequest(workflowInstanceId) { BookmarkId = bookmark.Id };
            await _workflowScheduler.ScheduleAtAsync(bookmark.Id, request, executeAt, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ClearScheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.ToList();
        
        // Select all Delay bookmarks.
        var delayBookmarks = bookmarkList.Filter<Delay>().ToList();
        
        // Select all StartAt bookmarks.
        var startAtBookmarks = bookmarkList.Filter<StartAt>().ToList();
        
        // Concatenate the filtered bookmarks.
        var bookmarksToUnSchedule = delayBookmarks.Concat(startAtBookmarks).ToList();

        // Unschedule each bookmark.
        foreach (var bookmark in bookmarksToUnSchedule) 
            await _workflowScheduler.UnscheduleAsync(bookmark.Id, cancellationToken);
    }
}