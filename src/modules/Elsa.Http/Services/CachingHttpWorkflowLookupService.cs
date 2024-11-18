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
            var details = "";
            try
            {
                var cachingOptions = cache.CachingOptions.Value;
                entry.SetSlidingExpiration(cachingOptions.CacheDuration);
                var expirationToken = cache.GetToken(cacheManager.GetTriggerChangeTokenKey(bookmarkHash));
                details += "expirationToken: " + expirationToken is null ? "null" : "not null";
                entry.AddExpirationToken(expirationToken);

                var result = await decoratedService.FindWorkflowAsync(bookmarkHash, cancellationToken);

                if (result == null)
                    return null;

                details += "workflowGraph: " + result?.WorkflowGraph is null ? "null" : "not null";
                var workflowGraph = result.WorkflowGraph!;
                details += "DefinitionId: " + workflowGraph?.Workflow?.Identity?.DefinitionId is null ? "null" : "not null";
                var changeTokenKey = cacheManager.GetWorkflowChangeTokenKey(workflowGraph.Workflow.Identity.DefinitionId);
                var changeToken = cache.GetToken(changeTokenKey);
                details += "changeToken: " + changeToken is null ? "null" : "not null";
                entry.AddExpirationToken(changeToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The workflow with bookmark hash {bh} has issues: {message}, details: {details}", bookmarkHash, ex.Message, details);
                return null;
            }
        });
    }
}
