using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Decorates an <see cref="IWorkflowDefinitionService"/> with caching capabilities.
/// </summary>
[UsedImplicitly]
public class CachingWorkflowDefinitionService(
    IWorkflowDefinitionService decoratedService, 
    IWorkflowDefinitionCacheManager cacheManager, 
    IWorkflowDefinitionStore workflowDefinitionStore,
    IMaterializerRegistry materializerRegistry) : IWorkflowDefinitionService
{
    /// <inheritdoc />
    public Task<WorkflowGraph> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) =>
        decoratedService.MaterializeWorkflowAsync(definition, cancellationToken);

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionId, versionOptions);

        return await FindFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionVersionId);
        return await FindFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionHandle handle, CancellationToken cancellationToken = default)
    {
        var filter = handle.ToFilter();
        return FindWorkflowDefinitionAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionFilterCacheKey(filter);
        return await FindFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowDefinitionAsync(filter, cancellationToken),
            x => x.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionId, versionOptions);
        return await FindFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken),
            x => x.Workflow.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionVersionId);
        return await FindFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowGraphAsync(definitionVersionId, cancellationToken),
            x => x.Workflow.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default)
    {
        var filter = definitionHandle.ToFilter();
        return FindWorkflowGraphAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowFilterCacheKey(filter);
        return await FindFromCacheAsync(cacheKey,
            () => decoratedService.FindWorkflowGraphAsync(filter, cancellationToken),
            x => x.Workflow.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowGraph>> FindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowDefinitions = await workflowDefinitionStore.FindManyAsync(filter, cancellationToken);
        var workflowGraphs = new List<WorkflowGraph>();
        foreach (var workflowDefinition in workflowDefinitions)
        {
            var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(workflowDefinition.Id);
            var workflowGraph = await GetFromCacheAsync(
                cacheKey,
                async () => await MaterializeWorkflowAsync(workflowDefinition, cancellationToken),
                wf => wf.Workflow.Identity.DefinitionId);
            workflowGraphs.Add(workflowGraph);
        }

        return workflowGraphs;
    }

    /// <inheritdoc />
    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionId, versionOptions);
        var result = await GetFromCacheAsync(cacheKey,
            () => decoratedService.TryFindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken),
            x => x.WorkflowDefinition?.DefinitionId);

        return result;
    }

    /// <inheritdoc />
    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionVersionId);
        var result = await GetFromCacheAsync(cacheKey,
            () => decoratedService.TryFindWorkflowGraphAsync(definitionVersionId, cancellationToken),
            x => x.WorkflowDefinition?.DefinitionId);

        return result;
    }

    /// <inheritdoc />
    public Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default)
    {
        var filter = definitionHandle.ToFilter();
        return TryFindWorkflowGraphAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowFilterCacheKey(filter);
        var result = await GetFromCacheAsync(cacheKey,
            () => decoratedService.TryFindWorkflowGraphAsync(filter, cancellationToken),
            x => x.WorkflowDefinition?.DefinitionId);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowGraphFindResult>> TryFindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowDefinitions = await workflowDefinitionStore.FindManyAsync(filter, cancellationToken);
        var results = new List<WorkflowGraphFindResult>();
        foreach (var workflowDefinition in workflowDefinitions)
        {
            var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(workflowDefinition.Id);
            var workflowGraph = await FindFromCacheAsync(
                cacheKey,
                async () => !materializerRegistry.IsMaterializerAvailable(workflowDefinition.MaterializerName) 
                    ? null 
                    : await MaterializeWorkflowAsync(workflowDefinition, cancellationToken),
                wf => wf.Workflow.Identity.DefinitionId);
            
            var result = new WorkflowGraphFindResult(workflowDefinition, workflowGraph);
            results.Add(result);
        }

        return results;
    }

    private async Task<T?> FindFromCacheAsync<T>(string cacheKey, Func<Task<T?>> getObjectFunc, Func<T, string> getChangeTokenKeyFunc) where T : class
    {
        return await GetFromCacheAsync(
            cacheKey,
            getObjectFunc,
            obj => obj != null ? getChangeTokenKeyFunc(obj) : null);
    }

    private async Task<T> GetFromCacheAsync<T>(string cacheKey, Func<Task<T>> getObjectFunc, Func<T, string?> getChangeTokenKeyFunc)
    {
        var cache = cacheManager.Cache;
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cache.CachingOptions.Value.CacheDuration);
            var obj = await getObjectFunc();
            var changeTokenKeyInput = getChangeTokenKeyFunc(obj);

            if (changeTokenKeyInput != null)
            {
                var changeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(changeTokenKeyInput);
                entry.AddExpirationToken(cache.GetToken(changeTokenKey));
            }

            return obj;
        });
    }
}