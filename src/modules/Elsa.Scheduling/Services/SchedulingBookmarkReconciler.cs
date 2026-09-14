using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Scheduling.Services;

/// <summary>
/// Classifies Delay/Timer/Cron/StartAt stored bookmarks against the workflow-instance store so startup
/// schedule rebuild can skip and purge bookmarks whose instance is missing or finished.
/// </summary>
public class SchedulingBookmarkReconciler(IWorkflowInstanceStore workflowInstanceStore, IBookmarkManager bookmarkManager)
{
    /// <summary>
    /// Splits bookmarks into those that may be scheduled and those whose instance is missing or terminal.
    /// </summary>
    public async Task<SchedulingBookmarkClassification> ClassifyAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks as IReadOnlyList<StoredBookmark> ?? bookmarks.ToList();

        if (bookmarkList.Count == 0)
            return new SchedulingBookmarkClassification([], []);

        var instanceIds = bookmarkList
            .Select(x => x.WorkflowInstanceId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var liveInstanceIds = instanceIds.Count == 0
            ? new HashSet<string>(StringComparer.Ordinal)
            : (await workflowInstanceStore.FindManyIdsAsync(new WorkflowInstanceFilter
            {
                Ids = instanceIds,
                WorkflowStatus = WorkflowStatus.Running
            }, cancellationToken)).ToHashSet(StringComparer.Ordinal);

        var schedulable = new List<StoredBookmark>(bookmarkList.Count);
        var orphans = new List<StoredBookmark>();

        foreach (var bookmark in bookmarkList)
        {
            if (!string.IsNullOrWhiteSpace(bookmark.WorkflowInstanceId) && liveInstanceIds.Contains(bookmark.WorkflowInstanceId))
                schedulable.Add(bookmark);
            else
                orphans.Add(bookmark);
        }

        return new SchedulingBookmarkClassification(schedulable, orphans);
    }

    /// <summary>
    /// Revalidates candidates against the workflow-instance store, then deletes bookmarks that are still orphans.
    /// </summary>
    public async Task PurgeAsync(IEnumerable<StoredBookmark> candidateBookmarks, CancellationToken cancellationToken = default)
    {
        var remainingOrphans = (await ClassifyAsync(candidateBookmarks, cancellationToken)).Orphans;
        var bookmarkIds = remainingOrphans
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (bookmarkIds.Count == 0)
            return;

        await bookmarkManager.DeleteManyAsync(new BookmarkFilter
        {
            BookmarkIds = bookmarkIds
        }, cancellationToken);
    }
}

/// <summary>
/// The schedulable vs orphan split for a batch of scheduling bookmarks.
/// </summary>
public record SchedulingBookmarkClassification(IReadOnlyList<StoredBookmark> Schedulable, IReadOnlyList<StoredBookmark> Orphans);
