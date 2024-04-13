using Elsa.Caching;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Specifies the contract for managing the cache of workflow definitions.
/// </summary>
public interface IWorkflowDefinitionCacheManager
{
    /// <summary>
    /// Gets the cache manager.
    /// </summary>
    ICacheManager Cache { get; }
    string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions);
    string CreateWorkflowDefinitionFilterCacheKey(WorkflowDefinitionFilter filter);
    string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions);
    string CreateWorkflowVersionCacheKey(string definitionVersionId);
    string CreateWorkflowFilterCacheKey(WorkflowDefinitionFilter filter);
    string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId);
    string CreateWorkflowDefinitionChangeTokenKey(string definitionId);
    Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
}