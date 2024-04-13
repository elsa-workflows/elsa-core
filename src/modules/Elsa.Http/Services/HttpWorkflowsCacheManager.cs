using Elsa.Caching;
using Elsa.Http.Contracts;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class HttpWorkflowsCacheManager(ICacheManager cache) : IHttpWorkflowsCacheManager
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
}