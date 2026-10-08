using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a contract for finding triggers and bookmarks associated with workflow activities.
/// </summary>
public interface IWorkflowMatcher
{
    /// <summary>
    /// Finds triggers associated with the specified activity type and stimulus.
    /// </summary>
    Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string activityTypeName, object stimulus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds triggers associated with the specified activity type and stimulus hash.
    /// </summary>
    Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string stimulusHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds bookmarks associated with the specified activity type and stimulus.
    /// </summary>
    Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(string activityTypeName, object stimulus, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds bookmarks associated with the specified activity type and stimulus hash.
    /// </summary>
    Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(string stimulusHash, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default);
}