using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// Bookmark payload for the <see cref="BulkDispatchWorkflows"/> activity.
/// </summary>
public class BulkDispatchWorkflowsBookmark(string parentInstanceId)
{
    /// <summary>
    /// The ID of the parent workflow instance that is waiting for child workflows to complete.
    /// </summary>
    public string ParentInstanceId { get; init; } = parentInstanceId;

    /// <summary>The number of child workflows that were created by the <see cref="BulkDispatchWorkflows"/> activity.</summary>
    [ExcludeFromHash] public long ScheduledInstanceIdsCount { get; set; }
    
}