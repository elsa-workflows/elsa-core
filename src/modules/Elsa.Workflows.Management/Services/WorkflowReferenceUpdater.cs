using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowReferenceUpdater(
    IWorkflowDefinitionPublisher publisher,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDefinitionStore workflowDefinitionStore,
    IApiSerializer serializer) : IWorkflowReferenceUpdater
{
    /// <inheritdoc />
    public async Task<UpdateWorkflowReferencesResult> UpdateWorkflowReferencesAsync(WorkflowDefinition referencedDefinition, CancellationToken cancellationToken = default)
    {
        // Skip if the published workflow definition is not usable as an activity or does not auto-update consuming workflows.
        if (referencedDefinition.Options is not { UsableAsActivity: true, AutoUpdateConsumingWorkflows: true })
            return new UpdateWorkflowReferencesResult([]);

        // Find all workflow graphs that contain the updated workflow definition.
        var consumingWorkflowGraphs = await FindWorkflowsContainingUpdatedWorkflowDefinitionAsync(referencedDefinition.DefinitionId, cancellationToken);

        // Update consuming workflows.
        var updatedWorkflows = new List<WorkflowDefinition>();
        foreach (var workflowGraph in consumingWorkflowGraphs)
        {
            var newDefinition = await UpdateConsumingWorkflowAsync(workflowGraph, referencedDefinition, cancellationToken);

            if (newDefinition != null)
                updatedWorkflows.Add(newDefinition);
        }

        return new UpdateWorkflowReferencesResult(updatedWorkflows);
    }

    private async Task<WorkflowDefinition?> UpdateConsumingWorkflowAsync(WorkflowGraph workflowGraph, WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        // Create a new version of the published workflow definition or get the existing draft.
        var consumerDefinitionId = workflowGraph.Workflow.Identity.DefinitionId;
        var originalVersionIsPublished = workflowGraph.Workflow.Publication.IsPublished;
        var newVersion = await publisher.GetDraftAsync(consumerDefinitionId, VersionOptions.LatestOrPublished, cancellationToken);

        // This is null in case the definition no longer exists in the store.
        if (newVersion == null)
            return null;

        // Materialize the draft to find all workflow definition activities that use the updated workflow definition.
        var newWorkflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(newVersion, cancellationToken);
        var outdatedWorkflowDefinitionActivities = FindOutdatedWorkflowDefinitionActivities(newWorkflowGraph, definition).ToList();

        // Skip if the new version of the published workflow definition is not used in the workflow or if the activity is already up to date.
        if (outdatedWorkflowDefinitionActivities.Count == 0)
            return null;

        // Update the consuming workflow graph to use the new version of the published workflow definition.
        foreach (var workflowDefinitionActivity in outdatedWorkflowDefinitionActivities)
        {
            workflowDefinitionActivity.WorkflowDefinitionVersionId = definition.Id;
            workflowDefinitionActivity.LatestAvailablePublishedVersionId = definition.Id;
            workflowDefinitionActivity.Version = definition.Version;
        }

        // Update the new version of the published workflow definition.
        if (newWorkflowGraph.Root.Activity is Workflow newWorkflow)
            newVersion.StringData = serializer.Serialize(newWorkflow.Root);

        // If the draft is new, publish it.
        if (originalVersionIsPublished)
            await publisher.PublishAsync(newVersion, cancellationToken);
        else
            await publisher.SaveDraftAsync(newVersion, cancellationToken);

        return newVersion;
    }

    private IEnumerable<WorkflowDefinitionActivity> FindOutdatedWorkflowDefinitionActivities(WorkflowGraph workflowGraph, WorkflowDefinition updatedDefinition)
    {
        return FindWorkflowActivityDefinitionActivityNodes(workflowGraph.Root)
            .Where(x => x.WorkflowDefinitionId == updatedDefinition.DefinitionId && x.WorkflowDefinitionVersionId != updatedDefinition.Id);
    }

    private async Task<IEnumerable<WorkflowGraph>> FindWorkflowsContainingUpdatedWorkflowDefinitionAsync(string updatedWorkflowDefinitionId, CancellationToken cancellationToken)
    {
        var allWorkflowGraphs = (await GetAllWorkflowGraphsAsync(cancellationToken)).ToList();
        var filteredWorkflowGraphs = allWorkflowGraphs
            .Where(x => FindWorkflowActivityDefinitionActivityNodes(x.Root).Any(y => y.WorkflowDefinitionId == updatedWorkflowDefinitionId))
            .ToList();

        return filteredWorkflowGraphs;
    }

    private IEnumerable<WorkflowDefinitionActivity> FindWorkflowActivityDefinitionActivityNodes(ActivityNode parent)
    {
        foreach (var child in parent.Children)
        {
            if (child.Activity is WorkflowDefinitionActivity workflowDefinitionActivity)
                yield return workflowDefinitionActivity;
            else
            {
                foreach (var grandChild in FindWorkflowActivityDefinitionActivityNodes(child))
                    yield return grandChild;
            }
        }
    }

    private async Task<IEnumerable<WorkflowGraph>> GetAllWorkflowGraphsAsync(CancellationToken cancellationToken)
    {
        var workflowDefinitionFilter = new WorkflowDefinitionFilter
        {
            VersionOptions = VersionOptions.LatestOrPublished,
            IsReadonly = false
        };
        var workflowDefinitionSummaries = await workflowDefinitionStore.FindSummariesAsync(workflowDefinitionFilter, cancellationToken);
        
        // If there are workflow definition summaries with the same definition ID, only take the latest version.
        workflowDefinitionSummaries = workflowDefinitionSummaries
            .GroupBy(x => x.DefinitionId)
            .Select(x => x.OrderByDescending(y => y.Version).First())
            .ToList();
        
        var workflowGraphs = new List<WorkflowGraph>();

        foreach (var workflowDefinitionSummary in workflowDefinitionSummaries)
        {
            var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionSummary.Id, cancellationToken);

            if (workflowGraph != null)
                workflowGraphs.Add(workflowGraph);
        }

        return workflowGraphs;
    }
}