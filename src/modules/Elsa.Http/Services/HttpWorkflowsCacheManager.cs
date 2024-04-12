using Elsa.Caching.Contracts;
using Elsa.Caching.Options;
using Elsa.Http.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
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
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : IHttpWorkflowsCacheManager
{
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
    public async Task EvictWorkflowAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = GetWorkflowChangeTokenKey(workflowDefinitionId);
        await changeTokenSignaler.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EvictTriggerAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var changeTokenKey = GetTriggerChangeTokenKey(bookmarkHash);
        await changeTokenSignaler.TriggerTokenAsync(changeTokenKey, cancellationToken);
    }

    private async Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string bookmarkHash, CancellationToken cancellationToken)
    {
        var triggerFilter = new TriggerFilter
        {
            Hash = bookmarkHash,
            TenantAgnostic = true
        };
        return await triggerStore.FindManyAsync(triggerFilter, cancellationToken);
    }

    private async Task<Workflow?> FindWorkflowAsync(StoredTrigger trigger, CancellationToken cancellationToken)
    {
        var workflowDefinitionVersionId = trigger.WorkflowDefinitionVersionId;
        return await workflowDefinitionService.FindWorkflowAsync(workflowDefinitionVersionId, false, cancellationToken);
    }

    private string GetWorkflowChangeTokenKey(string workflowDefinitionId) => $"{GetType().FullName}:workflow:{workflowDefinitionId}:changeToken";
    private string GetTriggerChangeTokenKey(string bookmarkHash) => $"{GetType().FullName}:trigger:{bookmarkHash}:changeToken";
}