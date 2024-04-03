using Elsa.Caching.Contracts;
using Elsa.Caching.Options;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class HttpWorkflowsCacheManager(
    IMemoryCache memoryCache,
    ITriggerStore triggerStore,
    IWorkflowDefinitionService workflowDefinitionService,
    IBookmarkHasher bookmarkHasher,
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : IHttpWorkflowsCacheManager
{
    private static readonly string HttpEndpointActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
    
    /// <inheritdoc />
    public string ComputeBookmarkHash(string path, string method)
    {
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        return bookmarkHasher.Hash(HttpEndpointActivityTypeName, bookmarkPayload);
    }

    /// <inheritdoc />
    public async Task<(Workflow? Workflow, ICollection<StoredTrigger> Triggers)?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var key = $"workflow:{bookmarkHash}";
        return await memoryCache.GetOrCreateAsync(key, async entry =>
        {
            entry.SetSlidingExpiration(cachingOptions.Value.CacheDuration);
            entry.AddExpirationToken(changeTokenSignaler.GetToken(GetTriggerChangeTokenKey(bookmarkHash)));
            
            var triggers = await FindTriggersAsync(bookmarkHash, cancellationToken).ToList();

            if (triggers.Count > 1)
                return (default, triggers);

            var trigger = triggers.SingleOrDefault();

            if (trigger == null)
                return default;

            var workflow = await FindWorkflowAsync(trigger, cancellationToken);

            if (workflow == null)
                return default;

            var changeTokenKey = GetWorkflowChangeTokenKey(workflow.Identity.DefinitionId);
            var changeToken = changeTokenSignaler.GetToken(changeTokenKey);
            entry.AddExpirationToken(changeToken);
            
            return (workflow, triggers);
        });
    }

    /// <inheritdoc />
    public void EvictWorkflow(string workflowDefinitionId)
    {
        var changeTokenKey = GetWorkflowChangeTokenKey(workflowDefinitionId);
        changeTokenSignaler.TriggerToken(changeTokenKey);
    }

    /// <inheritdoc />
    public void EvictTrigger(string bookmarkHash)
    {
        var changeTokenKey = GetTriggerChangeTokenKey(bookmarkHash);
        changeTokenSignaler.TriggerToken(changeTokenKey);
    }

    private async Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string bookmarkHash, CancellationToken cancellationToken)
    {
        var triggerFilter = new TriggerFilter
        {
            Hash = bookmarkHash
        };
        return await triggerStore.FindManyAsync(triggerFilter, cancellationToken);
    }

    private async Task<Workflow?> FindWorkflowAsync(StoredTrigger trigger, CancellationToken cancellationToken)
    {
        var workflowDefinitionId = trigger.WorkflowDefinitionVersionId;
        var workflowDefinition = await workflowDefinitionService.FindAsync(workflowDefinitionId, cancellationToken);

        if (workflowDefinition == null)
            return default;

        return await workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
    }

    private string GetWorkflowChangeTokenKey(string workflowDefinitionId) => $"{GetType().FullName}:workflow:{workflowDefinitionId}:changeToken";
    private string GetTriggerChangeTokenKey(string bookmarkHash) => $"{GetType().FullName}:trigger:{bookmarkHash}:changeToken";
}