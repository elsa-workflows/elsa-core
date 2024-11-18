using Elsa.Http.Contracts;
using Elsa.Http.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elsa.Http.Services;

/// <summary>
/// Represents a caching implementation of the IHttpWorkflowLookupService that retrieves workflows using HTTP workflow lookup service and caches the results.
/// </summary>
[UsedImplicitly]
public class CachingHttpWorkflowLookupService(
    IHttpWorkflowLookupService decoratedService,
    IHttpWorkflowsCacheManager cacheManager,
    ILogger<CachingHttpWorkflowLookupService> logger) : IHttpWorkflowLookupService
{
    /// <inheritdoc />
    public async Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var key = $"http-workflow:{bookmarkHash}";
        var cache = cacheManager.Cache;
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            try
            {
                var cachingOptions = cache.CachingOptions.Value;
                entry.SetSlidingExpiration(cachingOptions.CacheDuration);
                entry.AddExpirationToken(cache.GetToken(cacheManager.GetTriggerChangeTokenKey(bookmarkHash)));

                var result = await decoratedService.FindWorkflowAsync(bookmarkHash, cancellationToken);

                if (result == null)
                    return null;

                var workflowGraph = result.WorkflowGraph!;
                var changeTokenKey = cacheManager.GetWorkflowChangeTokenKey(workflowGraph.Workflow.Identity.DefinitionId);
                var changeToken = cache.GetToken(changeTokenKey);
                entry.AddExpirationToken(changeToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The workflow with bookmark hash {bh} has issues: {message}", bookmarkHash, ex.Message);
                return null;
            }
        });
    }
}