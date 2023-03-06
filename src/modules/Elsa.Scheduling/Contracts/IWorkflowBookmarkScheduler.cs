using Elsa.Workflows.Core.Models;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// Schedules jobs for the specified list of workflow bookmarks.
/// </summary>
public interface IWorkflowBookmarkScheduler
{
    Task ScheduleBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    Task UnscheduleBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
}