using Elsa.Common.Models;

namespace Elsa.Workflows.Management;

public interface IWorkflowDefinitionCacheManager
{
    string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions, bool tenantAgnostic);
    string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions, bool tenantAgnostic);
    string CreateWorkflowVersionCacheKey(string definitionVersionId, bool tenantAgnostic);
    string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId, bool tenantAgnostic);
    string CreateWorkflowDefinitionChangeTokenKey(string definitionId);
    Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
}