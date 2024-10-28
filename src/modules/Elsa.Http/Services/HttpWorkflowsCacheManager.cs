using Elsa.Caching;
using Elsa.Http.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class HttpWorkflowsCacheManager(ICacheManager cache, IHasher bookmarkHasher) : IHttpWorkflowsCacheManager
{
    /// <inheritdoc />
    public ICacheManager Cache => cache;

    /// <inheritdoc />
    public async Task EvictWorkflowAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = GetWorkflowChangeTokenKey(workflowDefinitionId);
        await cache.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EvictTriggerAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = GetTriggerChangeTokenKey(bookmarkHash);
        await cache.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }

    /// <inheritdoc />
    public string GetWorkflowChangeTokenKey(string workflowDefinitionId) => $"{GetType().FullName}:workflow:{workflowDefinitionId}:changeToken";

    /// <inheritdoc />
    public string GetTriggerChangeTokenKey(string bookmarkHash) => $"{GetType().FullName}:trigger:{bookmarkHash}:changeToken";

    /// <inheritdoc />
    public string ComputeBookmarkHash(string path, string method)
    {
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return bookmarkHasher.Hash(activityTypeName, bookmarkPayload);
    }
}