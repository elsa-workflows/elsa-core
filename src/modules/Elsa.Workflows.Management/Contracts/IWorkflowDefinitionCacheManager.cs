using Elsa.Common.Models;

namespace Elsa.Workflows.Management.Contracts;

public interface IWorkflowDefinitionCacheManager
{
    string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions);
    string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions);
    string CreateWorkflowVersionCacheKey(string definitionVersionId);
    string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId);
    string CreateWorkflowDefinitionChangeTokenKey(string definitionId);
    Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
}