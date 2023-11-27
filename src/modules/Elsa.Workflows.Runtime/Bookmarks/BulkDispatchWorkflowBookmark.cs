using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// Bookmark payload for the <see cref="BulkDispatchWorkflow"/> activity.
/// </summary>
/// <param name="ScheduledInstanceIdsCount">The number of child workflows that were created by the <see cref="BulkDispatchWorkflow"/> activity.</param>
public record BulkDispatchWorkflowBookmark(
    string ParentInstanceId,
    [property: ExcludeFromHash]long ScheduledInstanceIdsCount);