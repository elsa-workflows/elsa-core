using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a service that looks up bookmark-bound workflows.
/// </summary>
public interface IBookmarkBoundWorkflowService
{
    /// <summary>
    /// Finds bookmark-bound workflows by activity type name and stimulus.
    /// </summary>
    Task<IEnumerable<BookmarkBoundWorkflow>> FindManyAsync(string activityTypeName, object stimulus, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds bookmark-bound workflows by stimulus hash.
    /// </summary>
    Task<IEnumerable<BookmarkBoundWorkflow>> FindManyAsync(string stimulusHash, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default);
}