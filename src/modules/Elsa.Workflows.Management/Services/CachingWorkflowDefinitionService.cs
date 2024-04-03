using Elsa.Caching.Contracts;
using Elsa.Caching.Options;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
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
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionId, versionOptions);
        return await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            var definition = await decoratedService.FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);

            if (definition == null)
                return null;
            
            var definitionChangeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(definition.DefinitionId);
            entry.AddExpirationToken(changeTokenSignaler.GetToken(definitionChangeTokenKey));
            return definition;
        });
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowDefinitionVersionCacheKey(definitionVersionId);
        return await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            var definition = await decoratedService.FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken);
            
            if (definition == null)
                return null;

            var definitionChangeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(definition.DefinitionId);
            entry.AddExpirationToken(changeTokenSignaler.GetToken(definitionChangeTokenKey));
            return definition;
        });
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionId, versionOptions);
        return await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            var workflow = await decoratedService.FindWorkflowAsync(definitionId, versionOptions, cancellationToken);

            if (workflow == null)
                return null;

            var definitionChangeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(workflow.Identity.DefinitionId);
            entry.AddExpirationToken(changeTokenSignaler.GetToken(definitionChangeTokenKey));
            return workflow;
        });
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = cacheManager.CreateWorkflowVersionCacheKey(definitionVersionId);
        return await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(cachingOptions.Value.CacheDuration);
            var definition = await decoratedService.FindWorkflowAsync(definitionVersionId, cancellationToken);

            if (definition == null)
                return null;

            var definitionChangeTokenKey = cacheManager.CreateWorkflowDefinitionChangeTokenKey(definition.Identity.DefinitionId);
            entry.AddExpirationToken(changeTokenSignaler.GetToken(definitionChangeTokenKey));

            return definition;
        });
    }
}