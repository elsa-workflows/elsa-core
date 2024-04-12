using Elsa.Caching.Contracts;
using Elsa.Common.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionCacheManager(IChangeTokenSignaler changeTokenSignaler) : IWorkflowDefinitionCacheManager
{
    /// <inheritdoc />
    public string CreateWorkflowDefinitionVersionCacheKey(string definitionId, VersionOptions versionOptions, bool tenantAgnostic) => $"WorkflowDefinition:{definitionId}:{versionOptions}:{tenantAgnostic}";

    /// <inheritdoc />
    public string CreateWorkflowVersionCacheKey(string definitionId, VersionOptions versionOptions, bool tenantAgnostic) => $"Workflow:{definitionId}:{versionOptions}:{tenantAgnostic}";

    /// <inheritdoc />
    public string CreateWorkflowVersionCacheKey(string definitionVersionId, bool tenantAgnostic) => $"Workflow:{definitionVersionId}:{tenantAgnostic}";

    /// <inheritdoc />
    public string CreateWorkflowDefinitionVersionCacheKey(string definitionVersionId, bool tenantAgnostic) => $"WorkflowDefinition:{definitionVersionId}:{tenantAgnostic}";

    /// <inheritdoc />
    public string CreateWorkflowDefinitionChangeTokenKey(string definitionId) => $"WorkflowChangeToken:{definitionId}";

    /// <inheritdoc />
    public async Task EvictWorkflowDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = CreateWorkflowDefinitionChangeTokenKey(definitionId);
        await changeTokenSignaler.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }
}