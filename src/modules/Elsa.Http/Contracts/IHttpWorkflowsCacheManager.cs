using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Http.Contracts;

/// <summary>
/// Represents a service that caches HTTP workflows and triggers.
/// </summary>
public interface IHttpWorkflowsCacheManager
{
    /// <summary>
    /// Computes a bookmark hash for a given path and method.
    /// </summary>
    string ComputeBookmarkHash(string path, string method);
    
    /// <summary>
    /// Finds a cached entry by bookmark hash.
    /// </summary>
    Task<(Workflow? Workflow, ICollection<StoredTrigger> Triggers)?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Evicts a cached entry by its definition ID.
    /// </summary>
    Task EvictWorkflowAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Evicts a cached entry by its bookmark hash.
    /// </summary>
    Task EvictTriggerAsync(string bookmarkHash, CancellationToken cancellationToken = default);
}