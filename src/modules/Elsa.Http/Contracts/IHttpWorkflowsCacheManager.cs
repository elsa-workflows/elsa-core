using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Http.Contracts;

/// <summary>
/// Represents a service that caches HTTP workflows and triggers.
/// </summary>
public interface IHttpWorkflowsCacheManager
{
    string ComputeBookmarkHash(string path, string method);
    
    /// <summary>
    /// Finds a cached entry by bookmark hash.
    /// </summary>
    Task<(Workflow? Workflow, ICollection<StoredTrigger> Triggers)?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Evicts a cached entry by its definition ID.
    /// </summary>
    void EvictWorkflow(string workflowDefinitionId);
    
    /// <summary>
    /// Evicts a cached entry by its bookmark hash.
    /// </summary>
    void EvictTrigger(string bookmarkHash);
}