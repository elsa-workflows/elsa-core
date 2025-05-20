using Elsa.Caching;

namespace Elsa.Http;

/// <summary>
/// Represents a service that caches HTTP workflows and triggers.
/// </summary>
public interface IHttpWorkflowsCacheManager
{
    /// <summary>
    /// Gets the cache manager.
    /// </summary>
    public ICacheManager Cache { get; }
    
    /// <summary>
    /// Evicts a cached entry by its definition ID.
    /// </summary>
    Task EvictWorkflowAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evicts a cached entry by its bookmark hash.
    /// </summary>
    Task EvictTriggerAsync(string bookmarkHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the key for a workflow change token.
    /// </summary>
    string GetWorkflowChangeTokenKey(string workflowDefinitionId);

    /// <summary>
    /// Gets the key for a trigger change token.
    /// </summary>
    string GetTriggerChangeTokenKey(string bookmarkHash);
    
    /// <summary>
    /// Compute the bookmark hash for a given path and method combination.
    /// </summary>
    string ComputeBookmarkHash(string path, string method);
}