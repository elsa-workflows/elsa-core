using Elsa.Caching;
using Elsa.Common.Multitenancy;
using Elsa.Http.Bookmarks;
using Elsa.Workflows;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class HttpWorkflowsCacheManager(ICacheManager cache, IHasher bookmarkHasher, ITenantAccessor tenantAccessor) : IHttpWorkflowsCacheManager
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
    public string GetWorkflowChangeTokenKey(string workflowDefinitionId) => TenantPrefix() + $"{GetType().FullName}:workflow:{workflowDefinitionId}:changeToken";

    /// <inheritdoc />
    public string GetTriggerChangeTokenKey(string bookmarkHash) => TenantPrefix() + $"{GetType().FullName}:trigger:{bookmarkHash}:changeToken";

    /// <inheritdoc />
    public string ComputeBookmarkHash(string path, string method)
    {
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        return bookmarkHasher.Hash(HttpStimulusNames.HttpEndpoint, bookmarkPayload);
    }

    private string TenantPrefix()
    {
        var tenantId = tenantAccessor.TenantId;
        return string.IsNullOrEmpty(tenantId) ? string.Empty : $"{tenantId}:";
    }
}
