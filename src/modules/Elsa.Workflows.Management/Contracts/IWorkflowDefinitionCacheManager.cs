using Elsa.Caching;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management;

/// Specifies the contract for managing the cache of workflow definitions.
public interface IWorkflowDefinitionCacheManager
{
    /// Gets the cache manager.
    ICacheManager Cache { get; }

    /// Creates a cache key for the specified workflow definition ID and version options.
    string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions);

    /// Creates a cache key for the specified workflow definition filter.
    string CreateWorkflowDefinitionFilterCacheKey(WorkflowDefinitionFilter filter);

    /// Creates a cache key for the specified workflow definition ID.
    string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions);

    /// Creates a cache key for the specified workflow definition version ID.
    string CreateWorkflowVersionCacheKey(string definitionVersionId);

    /// Creates a cache key for the specified workflow definition filter.
    string CreateWorkflowFilterCacheKey(WorkflowDefinitionFilter filter);

    /// Creates a cache key for the specified workflow definition version ID.
    string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId);

    /// Creates a cache key for the specified workflow definition ID.
    string CreateWorkflowDefinitionChangeTokenKey(string definitionId);

    /// Evicts the specified workflow definition from the cache.
    Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
}