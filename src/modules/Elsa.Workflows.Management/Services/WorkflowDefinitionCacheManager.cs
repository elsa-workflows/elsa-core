using Elsa.Caching.Contracts;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionCacheManager(IMemoryCache memoryCache, IChangeTokenSignaler changeTokenSignaler) : IWorkflowDefinitionCacheManager
{
    /// <inheritdoc />
    public string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions) => $"WorkflowDefinition:{definitionId}:{versionOptions}";

    /// <inheritdoc />
    public string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions) => $"Workflow:{definitionId}:{versionOptions}";

    /// <inheritdoc />
    public string CreateWorkflowVersionCacheKey(string definitionVersionId) => $"Workflow:{definitionVersionId}";

    /// <inheritdoc />
    public string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId) => $"WorkflowDefinition:{definitionVersionId}";

    /// <inheritdoc />
    public string CreateWorkflowDefinitionChangeTokenKey(string definitionId) => $"WorkflowChangeToken:{definitionId}";

    /// <inheritdoc />
    public string CreateWorkflowDefinitionVersionChangeTokenKey(string definitionVersionId) => $"WorkflowChangeToken:{definitionVersionId}";

    /// <inheritdoc />
    public Task EvictWorkflowDefinitionVersionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = CreateWorkflowDefinitionVersionCacheKey(definitionId, versionOptions);
        memoryCache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task EvictWorkflowDefinitionVersionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = CreateWorkflowDefinitionVersionChangeTokenKey(definitionVersionId);
        await changeTokenSignaler.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = CreateWorkflowDefinitionChangeTokenKey(definitionId);
        await changeTokenSignaler.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }
}