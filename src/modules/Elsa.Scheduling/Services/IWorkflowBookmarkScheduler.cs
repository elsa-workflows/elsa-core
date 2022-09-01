using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Scheduling.Services;

/// <summary>
/// Schedules jobs for the specified list of workflow bookmarks.
/// </summary>
public interface IWorkflowBookmarkScheduler
{
    Task ScheduleBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    Task UnscheduleBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
}