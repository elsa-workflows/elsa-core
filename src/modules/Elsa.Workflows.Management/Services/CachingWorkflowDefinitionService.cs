using Elsa.Caching.Contracts;
using Elsa.Caching.Options;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Decorates an <see cref="IWorkflowDefinitionService"/> with caching capabilities.
/// </summary>
[UsedImplicitly]
public class CachingWorkflowDefinitionService(
    IWorkflowDefinitionService decoratedService,
    IWorkflowDefinitionCacheManager cacheManager,
    IMemoryCache memoryCache,
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : IWorkflowDefinitionService
{
    /// <inheritdoc />
    public async Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
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
    public async Task<Workflow?> FindWorkflowAsync(string definitionId, VersionOptions versionOptions, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionId, versionOptions, tenantAgnostic);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowAsync(definitionId, versionOptions, tenantAgnostic, cancellationToken),
            x => x.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(string definitionVersionId, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionVersionId, tenantAgnostic);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowAsync(definitionVersionId, tenantAgnostic, cancellationToken),
            x => x.Identity.DefinitionId);
    }

    private async Task<T?> GetFromCacheAsync<T>(string cacheKey, Func<Task<T?>> getObjectFunc, Func<T, string> getChangeTokenKeyFunc)
    {
        return await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            var obj = await getObjectFunc();

            if (obj == null)
                return default;

            var changeTokenKeyInput = getChangeTokenKeyFunc(obj);
            var changeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(changeTokenKeyInput);
            entry.AddExpirationToken(changeTokenSignaler.GetToken(changeTokenKey));
            return obj;
        });
    }
}