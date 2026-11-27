using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Stores;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Http.Services;

/// <summary>
/// Represents a caching implementation of the IHttpWorkflowLookupService that retrieves workflows using HTTP workflow lookup service and caches the results.
/// </summary>
[UsedImplicitly]
public class CachingHttpWorkflowLookupService(
    IHttpWorkflowLookupService decoratedService,
    IHttpWorkflowsCacheManager cacheManager,
    ITenantAccessor tenantAccessor) : IHttpWorkflowLookupService
{
    /// <inheritdoc />
    public async Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var tenant = tenantAccessor.Tenant;
        var tenantId = tenant?.Id;
        var tenantIdPrefix = !string.IsNullOrEmpty(tenantId) ? $"{tenantId}:" : string.Empty;
        var key = $"{tenantIdPrefix}http-workflow:{bookmarkHash}";
        var cache = cacheManager.Cache;
        return await cache.FindOrCreateAsync(key, async entry =>
        {
            var cachingOptions = cache.CachingOptions.Value;
            entry.SetSlidingExpiration(cachingOptions.CacheDuration);
            entry.AddExpirationToken(cache.GetToken(cacheManager.GetTriggerChangeTokenKey(bookmarkHash)));
            entry.AddExpirationToken(cache.GetToken(CachingTriggerStore.RequiredRemovalCacheInvalidationTokenKey));

            // Taken before the lookup, so that a trigger change during the lookup expires a "not found" result.
            var triggerChangeToken = cache.GetToken(CachingTriggerStore.CacheInvalidationTokenKey);
            var result = await decoratedService.FindWorkflowAsync(bookmarkHash, cancellationToken);

            // A tenant finds no workflow while it overrides the tenant-agnostic workflow of the route; any trigger change can end that.
            if (result == null)
            {
                entry.AddExpirationToken(triggerChangeToken);
                return null;
            }
            
            if(result.WorkflowGraph == null)
                return result;

            var workflowGraph = result.WorkflowGraph!;
            var changeTokenKey = cacheManager.GetWorkflowChangeTokenKey(workflowGraph.Workflow.Identity.DefinitionId);
            var changeToken = cache.GetToken(changeTokenKey);
            entry.AddExpirationToken(changeToken);

            return result;
        });
    }
}