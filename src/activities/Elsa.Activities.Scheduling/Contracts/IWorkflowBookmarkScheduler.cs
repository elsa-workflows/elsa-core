using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Activities.Scheduling.Contracts;

/// <summary>
/// Schedules jobs for the specified list of workflow bookmarks.
/// </summary>
public interface IWorkflowBookmarkScheduler
{
    Task ScheduleBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
}