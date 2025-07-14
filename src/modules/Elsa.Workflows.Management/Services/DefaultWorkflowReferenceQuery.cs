using Elsa.Common.Models;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

public class DefaultWorkflowReferenceQuery(IWorkflowDefinitionService workflowDefinitionService, IWorkflowDefinitionStore workflowDefinitionStore) : IWorkflowReferenceQuery
{
    public async Task<IEnumerable<string>> ExecuteAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var workflowGraphs = await FindWorkflowsContainingUpdatedWorkflowDefinitionAsync(workflowDefinitionId, cancellationToken);
        return workflowGraphs.Select(x => x.Workflow.Identity.DefinitionId).Distinct();
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