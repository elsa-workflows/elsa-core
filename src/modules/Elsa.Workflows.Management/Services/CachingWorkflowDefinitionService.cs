using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Decorates an <see cref="IWorkflowDefinitionService"/> with caching capabilities.
/// </summary>
[UsedImplicitly]
public class CachingWorkflowDefinitionService(IWorkflowDefinitionService decoratedService, IWorkflowDefinitionCacheManager cacheManager) : IWorkflowDefinitionService
{
    /// <inheritdoc />
    public async Task<WorkflowGraph> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        return await decoratedService.MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionId, versionOptions, tenantAgnostic);

        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(definitionId, versionOptions, tenantAgnostic, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionVersionId, tenantAgnostic);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(definitionVersionId, tenantAgnostic, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionFilterCacheKey(filter);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(filter, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionId, versionOptions);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowGraphAsync(definitionId, versionOptions, tenantAgnostic, cancellationToken),
            x => x.Workflow.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionVersionId, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionVersionId, tenantAgnostic);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowGraphAsync(definitionVersionId, tenantAgnostic, cancellationToken),
            x => x.Workflow.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowFilterCacheKey(filter);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowGraphAsync(filter, cancellationToken),
            x => x.Workflow.Identity.DefinitionId);
    }

    private async Task<T?> GetFromCacheAsync<T>(string cacheKey, Func<Task<T?>> getObjectFunc, Func<T, string> getChangeTokenKeyFunc)
    {
        var cache = cacheManager.Cache;
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cache.CachingOptions.Value.CacheDuration);
            var obj = await getObjectFunc();

            if (obj == null)
                return default;

            var changeTokenKeyInput = getChangeTokenKeyFunc(obj);
            var changeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(changeTokenKeyInput);
            entry.AddExpirationToken(cache.GetToken(changeTokenKey));
            return obj;
        });
    }
}