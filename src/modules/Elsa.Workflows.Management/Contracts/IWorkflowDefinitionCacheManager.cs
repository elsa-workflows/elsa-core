using Elsa.Caching;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management;

/// <summary>
/// Specifies the contract for managing the cache of workflow definitions.
/// </summary>
public interface IWorkflowDefinitionCacheManager
{
    /// <summary>
    /// Gets the cache manager.
    /// </summary>
    ICacheManager Cache { get; }

    /// <summary>
    /// Creates a cache key for the specified workflow definition ID and version options.
    /// </summary>
    string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions);

    /// <summary>
    /// Creates a cache key for the specified workflow definition filter.
    /// </summary>
    string CreateWorkflowDefinitionFilterCacheKey(WorkflowDefinitionFilter filter);

    /// <summary>
    /// Creates a cache key for the specified workflow definition ID.
    /// </summary>
    string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions);

    /// <summary>
    /// Creates a cache key for the specified workflow definition version ID.
    /// </summary>
    string CreateWorkflowVersionCacheKey(string definitionVersionId);

    /// <summary>
    /// Creates a cache key for the specified workflow definition filter.
    /// </summary>
    string CreateWorkflowFilterCacheKey(WorkflowDefinitionFilter filter);

    /// <summary>
    /// Creates a cache key for the specified workflow definition version ID.
    /// </summary>
    string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId);

    /// <summary>
    /// Creates a cache key for the specified workflow definition ID.
    /// </summary>
    string CreateWorkflowDefinitionChangeTokenKey(string definitionId);

    /// <summary>
    /// Evicts the specified workflow definition from the cache.
    /// </summary>
    Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
}