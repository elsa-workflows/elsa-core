using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Provides access to stored bookmarks.
/// </summary>
public interface IBookmarkStore
{
    /// <summary>
    /// Adds or updates the specified bookmark. 
    /// </summary>
    ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a set of bookmarks matching the specified workflow instance ID.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a set of bookmarks matching the specified hash.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindByHashAsync(string hash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a set of bookmarks matching the specified workflow instance ID and hash.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAndHashAsync(string workflowInstanceId, string hash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a set of bookmarks matching the specified correlation ID and hash.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindByCorrelationAndHashAsync(string correlationId, string hash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a set of bookmarks matching the specified workflow instance ID and hash.
    /// </summary>
    ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a set of bookmarks matching the specified activity type.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default);
}