using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// Creates timer schedules for the specified bookmarks.
/// </summary>
public interface IBookmarkScheduler
{
    /// <summary>
    /// Schedules the specified list of bookmarks.
    /// </summary>
    /// <param name="bookmarks">The bookmarks to schedule.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ScheduleAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules the specified list of bookmarks.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to which the bookmarks belong.</param>
    /// <param name="bookmarks">The bookmarks to schedule.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ScheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unschedules the specified list of bookmarks.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to which the bookmarks belong.</param>
    /// <param name="bookmarks">The bookmarks to unschedule.</param>
    /// <param name="cancellationToken"></param>
    Task UnscheduleAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
}