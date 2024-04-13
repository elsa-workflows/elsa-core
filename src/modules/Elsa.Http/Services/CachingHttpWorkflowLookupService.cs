using Elsa.Caching;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Http.Services;

/// <summary>
/// Represents a caching implementation of the IHttpWorkflowLookupService that retrieves workflows using HTTP workflow lookup service and caches the results.
/// </summary>
[UsedImplicitly]
public class CachingHttpWorkflowLookupService(
    IHttpWorkflowLookupService decoratedService,
    IHttpWorkflowsCacheInvalidationManager cacheInvalidationManager,
    ICacheManager cacheManager) : IHttpWorkflowLookupService
{
    /// <inheritdoc />
    public async Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var key = $"http-workflow:{bookmarkHash}";
        return await cacheManager.GetOrCreateAsync(key, async entry =>
        {
            var cachingOptions = cacheManager.CachingOptions.Value;
            var changeTokenSignaler = cacheManager.ChangeTokenSignaler;
            entry.SetSlidingExpiration(cachingOptions.CacheDuration);
            entry.AddExpirationToken(changeTokenSignaler.GetToken(cacheInvalidationManager.GetTriggerChangeTokenKey(bookmarkHash)));

            var result = await decoratedService.FindWorkflowAsync(bookmarkHash, cancellationToken);

            if (result == null)
                return null;

            var workflow = result.Workflow!;
            var changeTokenKey = cacheInvalidationManager.GetWorkflowChangeTokenKey(workflow.Identity.DefinitionId);
            var changeToken = changeTokenSignaler.GetToken(changeTokenKey);
            entry.AddExpirationToken(changeToken);

            return result;
        });
    }
}