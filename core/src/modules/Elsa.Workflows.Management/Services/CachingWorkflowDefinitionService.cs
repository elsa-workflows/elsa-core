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
    public Task<WorkflowGraph> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        return decoratedService.MaterializeWorkflowAsync(definition, cancellationToken);
    }

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
        // Resolve the definition first (already cached by FindWorkflowDefinitionAsync) so the
        // WorkflowGraph ends up under the stable per-version-ID key rather than a filter-hash key
        // that can never be shared with the other FindWorkflowGraphAsync overloads.
        var definition = await FindWorkflowDefinitionAsync(filter, cancellationToken);
        return await FindWorkflowGraphForDefinitionAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowGraph>> FindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowDefinitions = await workflowDefinitionStore.FindManyAsync(filter, cancellationToken);
        var workflowGraphs = new List<WorkflowGraph>();
        foreach (var workflowDefinition in workflowDefinitions)
        {
            var workflowGraph = await FindWorkflowGraphForDefinitionAsync(workflowDefinition, cancellationToken);
            if (workflowGraph != null)
                workflowGraphs.Add(workflowGraph);
        }

        return workflowGraphs;
    }

    /// <inheritdoc />
    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);
        var workflowGraph = await FindWorkflowGraphForDefinitionAsync(definition, cancellationToken);
        return new(definition, workflowGraph);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken);
        var workflowGraph = await FindWorkflowGraphForDefinitionAsync(definition, cancellationToken);
        return new(definition, workflowGraph);
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
        var definition = await FindWorkflowDefinitionAsync(filter, cancellationToken);
        var workflowGraph = await FindWorkflowGraphForDefinitionAsync(definition, cancellationToken);
        return new(definition, workflowGraph);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowGraphFindResult>> TryFindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowDefinitions = await workflowDefinitionStore.FindManyAsync(filter, cancellationToken);
        var results = new List<WorkflowGraphFindResult>();
        foreach (var workflowDefinition in workflowDefinitions)
        {
            var workflowGraph = await FindWorkflowGraphForDefinitionAsync(workflowDefinition, cancellationToken);
            results.Add(new WorkflowGraphFindResult(workflowDefinition, workflowGraph));
        }

        return results;
    }

    private async Task<T?> FindFromCacheAsync<T>(string cacheKey, Func<Task<T?>> getObjectFunc, Func<T, string> getChangeTokenKeyFunc) where T : class
    {
        var cache = cacheManager.Cache;
        return await cache.FindOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cache.CachingOptions.Value.CacheDuration);
            var obj = await getObjectFunc();

            if (obj != null)
            {
                var changeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(getChangeTokenKeyFunc(obj));
                entry.AddExpirationToken(cache.GetToken(changeTokenKey));
            }

            return obj;
        });
    }

    /// <summary>
    /// Returns the <see cref="WorkflowGraph"/> for the given definition, using the per-version-ID cache entry
    /// that is shared with <see cref="FindWorkflowGraphAsync(string,CancellationToken)"/>.
    /// Returns <c>null</c> when the definition is <c>null</c> or its materializer is unavailable.
    /// </summary>
    private async Task<WorkflowGraph?> FindWorkflowGraphForDefinitionAsync(WorkflowDefinition? definition, CancellationToken cancellationToken)
    {
        if (definition == null)
            return null;

        if (!materializerRegistry.IsMaterializerAvailable(definition.MaterializerName))
            return null;

        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definition.Id);
        var cache = cacheManager.Cache;
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cache.CachingOptions.Value.CacheDuration);
            var graph = await MaterializeWorkflowAsync(definition, cancellationToken);
            var changeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(graph.Workflow.Identity.DefinitionId);
            entry.AddExpirationToken(cache.GetToken(changeTokenKey));
            return graph;
        });
    }

}