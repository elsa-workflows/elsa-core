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
    string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions, bool tenantAgnostic);
    string CreateWorkflowDefinitionFilterCacheKey(WorkflowDefinitionFilter filter);
    string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions, bool tenantAgnostic);
    string CreateWorkflowVersionCacheKey(string definitionVersionId, bool tenantAgnostic);
    string CreateWorkflowFilterCacheKey(WorkflowDefinitionFilter filter);
    string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId, bool tenantAgnostic);
    string CreateWorkflowDefinitionChangeTokenKey(string definitionId, bool tenantAgnostic);
    Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
}