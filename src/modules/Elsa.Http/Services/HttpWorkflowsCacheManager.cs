using Elsa.Common.Contracts;
using Elsa.Common.Options;
using Elsa.Http.Contracts;
using Elsa.Workflows.Activities;
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
    IChangeTokenSignaler changeTokenSignaler,
    IOptions<CachingOptions> cachingOptions) : IHttpWorkflowsCacheManager
{
    /// <inheritdoc />
    public async Task<(Workflow? Workflow, ICollection<StoredTrigger> Triggers)?> FindCachedWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var key = $"workflow:{bookmarkHash}";
        return await memoryCache.GetOrCreateAsync(key, async entry =>
        {
            var triggers = await FindTriggersAsync(bookmarkHash, cancellationToken).ToList();

            if (triggers.Count > 1)
                return (default, triggers);

            var trigger = triggers.SingleOrDefault();

            if (trigger == null)
                return default;

            var workflow = await FindWorkflowAsync(trigger, cancellationToken);

            if (workflow == null)
                return default;

            var changeTokenKey = GetChangeTokenKey(workflow.Identity.DefinitionId);
            var changeToken = changeTokenSignaler.GetToken(changeTokenKey);
            entry.AddExpirationToken(changeToken);
            entry.SetSlidingExpiration(cachingOptions.Value.CacheDuration);

            return (workflow, triggers);
        });
    }

    /// <inheritdoc />
    public void EvictCachedWorkflow(string workflowDefinitionId)
    {
        var changeTokenKey = GetChangeTokenKey(workflowDefinitionId);
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

    private string GetChangeTokenKey(string workflowDefinitionId) => $"workflow:{workflowDefinitionId}:changeToken";
}