using Elsa.Caching;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionCacheManager(ICacheManager cache, IHasher hasher) : IWorkflowDefinitionCacheManager
{
    /// <inheritdoc />
    public ICacheManager Cache => cache;

    /// <inheritdoc />
    public string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions) => $"WorkflowDefinition:{definitionId}:{versionOptions}";

    /// <inheritdoc />
    public string CreateWorkflowDefinitionFilterCacheKey(WorkflowDefinitionFilter filter)
    {
        var hash = hasher.Hash(filter);
        return $"WorkflowDefinition:{hash}";
    }

    /// <inheritdoc />
    public string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions) => $"Workflow:{definitionId}:{versionOptions}";

    /// <inheritdoc />
    public string CreateWorkflowVersionCacheKey(string definitionVersionId) => $"Workflow:{definitionVersionId}";

    /// <inheritdoc />
    public string CreateWorkflowFilterCacheKey(WorkflowDefinitionFilter filter)
    {
        var hash = hasher.Hash(filter);
        return $"Workflow:{hash}";
    }

    /// <inheritdoc />
    public string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId) => $"WorkflowDefinition:{definitionVersionId}";

    /// <inheritdoc />
    public string CreateWorkflowDefinitionChangeTokenKey(string definitionId) => $"WorkflowChangeToken:{definitionId}";

    /// <inheritdoc />
    public async Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = CreateWorkflowDefinitionChangeTokenKey(definitionId);
        await cache.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }
}