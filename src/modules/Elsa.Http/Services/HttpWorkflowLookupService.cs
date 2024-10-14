using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Open.Linq.AsyncExtensions;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class HttpWorkflowLookupService(ITriggerStore triggerStore, IWorkflowDefinitionService workflowDefinitionService) : IHttpWorkflowLookupService
{
    /// <inheritdoc />
    public async Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default)
    {
        var triggers = await FindTriggersAsync(bookmarkHash, cancellationToken).ToList();

        if (triggers.Count > 1)
            return new(null, triggers);

        var trigger = triggers.SingleOrDefault();

        if (trigger == null)
            return default;

        var workflowGraph = await FindWorkflowGraphAsync(trigger, cancellationToken);

        if (workflowGraph == null)
            return default;

        return new(workflowGraph, triggers);
    }

    private async Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string bookmarkHash, CancellationToken cancellationToken)
    {
        var triggerFilter = new TriggerFilter
        {
            Hash = bookmarkHash
        };
        return await triggerStore.FindManyAsync(triggerFilter, cancellationToken);
    }

    private async Task<WorkflowGraph?> FindWorkflowGraphAsync(StoredTrigger trigger, CancellationToken cancellationToken)
    {
        var workflowDefinitionVersionId = trigger.WorkflowDefinitionVersionId;
        var filter = new WorkflowDefinitionFilter
        {
            Id = workflowDefinitionVersionId
        };
        return await workflowDefinitionService.FindWorkflowGraphAsync(filter, cancellationToken);
    }
}