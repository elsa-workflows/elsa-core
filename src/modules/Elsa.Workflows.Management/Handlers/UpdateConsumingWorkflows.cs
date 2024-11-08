using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// Updates consuming workflows when a workflow definition is published.
/// </summary>
public class UpdateConsumingWorkflows(
    IWorkflowDefinitionPublisher publisher, 
    IWorkflowDefinitionService workflowDefinitionService, 
    IWorkflowDefinitionStore workflowDefinitionStore,
    IApiSerializer serializer) : INotificationHandler<WorkflowDefinitionPublished>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        var definition = notification.WorkflowDefinition;

        // Skip if the published workflow definition is not usable as an activity or does not auto-update consuming workflows.
        if (definition.Options is not { UsableAsActivity: true, AutoUpdateConsumingWorkflows: true })
            return;

        // Build a network of workflow graphs.
        var allWorkflowDefinitionGraphs = await GetAllWorkflowGraphsAsync(cancellationToken);
        var definitionId = definition.DefinitionId;

        // Find all root workflow definition activity nodes.
        foreach (var workflowGraph in allWorkflowDefinitionGraphs)
        {
            var consumerDefinitionId = workflowGraph.Workflow.Identity.DefinitionId;

            var workflowDefinitionActivities =
                FindWorkflowActivityDefinitionActivityNodes(workflowGraph.Root)
                    .Where(x => x.WorkflowDefinitionId == definitionId)
                    .ToList();
            
            // Skip if the published workflow definition is not used in any workflow graph.
            if (workflowDefinitionActivities.Count == 0)
                continue;
            
            // Create a new version of the published workflow definition.
            var originalVersionIsPublished = workflowGraph.Workflow.Publication.IsPublished;
            var newVersion = await publisher.GetDraftAsync(consumerDefinitionId, VersionOptions.LatestOrPublished, cancellationToken);
            
            if(newVersion == null)
                continue;
            
            var newWorkflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(newVersion, cancellationToken);
            var newWorkflowDefinitionActivities =
                FindWorkflowActivityDefinitionActivityNodes(newWorkflowGraph.Root)
                    .Where(x => x.WorkflowDefinitionId == definitionId)
                    .ToList();

            foreach (var workflowDefinitionActivity in newWorkflowDefinitionActivities)
            {
                // Update the consuming workflow graph to use the new version of the published workflow definition.
                workflowDefinitionActivity.WorkflowDefinitionVersionId = definition.Id;
                workflowDefinitionActivity.LatestAvailablePublishedVersionId = definition.Id;
            }
            
            // Update the new version of the published workflow definition.
            if(newWorkflowGraph.Root.Activity is Workflow newWorkflow)
                newVersion.StringData = serializer.Serialize(newWorkflow.Root);

            // If the draft is new, publish it.
            if (originalVersionIsPublished)
                await publisher.PublishAsync(newVersion, cancellationToken);
            else
                await publisher.SaveDraftAsync(newVersion, cancellationToken);
        }
    }

    private IEnumerable<WorkflowDefinitionActivity> FindWorkflowActivityDefinitionActivityNodes(ActivityNode parent)
    {
        foreach (var child in parent.Children)
        {
            if (child.Activity is WorkflowDefinitionActivity workflowDefinitionActivity)
                yield return workflowDefinitionActivity;

            foreach (var grandChild in FindWorkflowActivityDefinitionActivityNodes(child))
                yield return grandChild;
        }
    }

    private async Task<IEnumerable<WorkflowGraph>> GetAllWorkflowGraphsAsync(CancellationToken cancellationToken)
    {
        var workflowDefinitionFilter = new WorkflowDefinitionFilter
        {
            VersionOptions = VersionOptions.LatestOrPublished
        };
        var workflowDefinitionSummaries = await workflowDefinitionStore.FindSummariesAsync(workflowDefinitionFilter, cancellationToken);
        var workflowGraphs = new List<WorkflowGraph>();

        foreach (var workflowDefinitionSummary in workflowDefinitionSummaries)
        {
            var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionSummary.DefinitionId, VersionOptions.LatestOrPublished, cancellationToken);

            if (workflowGraph != null)
                workflowGraphs.Add(workflowGraph);
        }

        return workflowGraphs;
    }
}