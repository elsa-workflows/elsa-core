using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class TriggerBoundWorkflowService(IWorkflowMatcher workflowMatcher, IWorkflowDefinitionService workflowDefinitionService, ILogger<TriggerBoundWorkflowService> logger) : ITriggerBoundWorkflowService
{
    /// <inheritdoc />
    public async Task<IEnumerable<TriggerBoundWorkflow>> FindManyAsync(string activityTypeName, object stimulus, CancellationToken cancellationToken = default)
    {
        var triggers = await workflowMatcher.FindTriggersAsync(activityTypeName, stimulus, cancellationToken);
        return await FindManyAsync(triggers, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TriggerBoundWorkflow>> FindManyAsync(string stimulusHash, CancellationToken cancellationToken = default)
    {
        var triggers = await workflowMatcher.FindTriggersAsync(stimulusHash, cancellationToken);
        return await FindManyAsync(triggers, cancellationToken);
    }

    private async Task<IEnumerable<TriggerBoundWorkflow>> FindManyAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var groupedTriggers = triggers.GroupBy(x => x.WorkflowDefinitionVersionId);
        var triggerBoundWorkflows = new List<TriggerBoundWorkflow>();

        foreach (var triggerGroup in groupedTriggers)
        {
            var workflowDefinitionVersionId = triggerGroup.Key;
            var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowDefinitionVersionId, cancellationToken);

            if (workflow == null)
            {
                logger.LogWarning("Workflow definition with ID {WorkflowDefinitionVersionId} not found", workflowDefinitionVersionId);
                continue;
            }

            var triggerBoundWorkflow = new TriggerBoundWorkflow(workflow, triggerGroup.ToList());
            triggerBoundWorkflows.Add(triggerBoundWorkflow);
        }

        return triggerBoundWorkflows;
    }
}