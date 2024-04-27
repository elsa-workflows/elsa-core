using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
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
    public async Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        return await decoratedService.MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionId, versionOptions);

        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionVersionId);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionHandle handle, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionHandle = handle
        };
        
        return FindWorkflowDefinitionAsync(filter, cancellationToken);
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
    public async Task<Workflow?> FindWorkflowAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionId, versionOptions);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowAsync(definitionId, versionOptions, cancellationToken),
            x => x.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionVersionId);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowAsync(definitionVersionId, cancellationToken),
            x => x.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public Task<Workflow?> FindWorkflowAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionHandle = definitionHandle
        };
        
        return FindWorkflowAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowFilterCacheKey(filter);
        return await GetFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowAsync(filter, cancellationToken),
            x => x.Identity.DefinitionId);
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