using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
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

        var workflow = await FindWorkflowAsync(trigger, cancellationToken);

        if (workflow == null)
            return default;

        return new(workflow, triggers);
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
        var workflowDefinitionVersionId = trigger.WorkflowDefinitionVersionId;
        return await workflowDefinitionService.FindWorkflowAsync(workflowDefinitionVersionId, cancellationToken);
    }
}