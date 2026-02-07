using Elsa.Common.Multitenancy;
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

            var result = await decoratedService.FindWorkflowAsync(bookmarkHash, cancellationToken);

            if (result == null)
                return null;
            
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