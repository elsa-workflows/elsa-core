using Elsa.Common.Models;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowGraphNetworkBuilder(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowDefinitionService workflowDefinitionService) : IWorkflowGraphNetworkBuilder
{
    /// <inheritdoc />
    public async Task<WorkflowGraphNetwork> BuildAsync(CancellationToken cancellationToken = default)
    {
        var workflowGraphs = (await GetAllWorkflowGraphsAsync(cancellationToken)).ToList();
        var nodes = new HashSet<WorkflowGraphNode>();
        
        var context = new WorkflowGraphNetworkBuilderContext
        {
            WorkflowGraphs = workflowGraphs,
            VisitedNodes = nodes
        };

        foreach (var workflowGraph in context.WorkflowGraphs) 
            VisitWorkflowGraph(workflowGraph, context);
        
        return new WorkflowGraphNetwork(nodes);
    }

    private void VisitWorkflowGraph(WorkflowGraph workflowGraph, WorkflowGraphNetworkBuilderContext context)
    {
        var activityNodes = workflowGraph.Nodes;
        var node = context.VisitedNodes.FirstOrDefault(x => x.WorkflowGraph == workflowGraph);

        if (node == null)
        {
            node = new WorkflowGraphNode(workflowGraph);
            context.VisitedNodes.Add(node);
        }

        var workflowDefinitionNodes = activityNodes
            .Where(x => x.Activity is WorkflowDefinitionActivity)
            .Select(x => (WorkflowDefinitionActivity)x.Activity)
            .ToList();

        foreach (var workflowDefinitionNode in workflowDefinitionNodes)
        {
            VisitConsumingActivityNode(node, workflowDefinitionNode, context);
        }
    }

    private void VisitConsumingActivityNode(WorkflowGraphNode node, WorkflowDefinitionActivity workflowDefinitionNode, WorkflowGraphNetworkBuilderContext context)
    {
        var consumedWorkflowDefinitionId = workflowDefinitionNode.WorkflowDefinitionId;
        var consumedWorkflowGraph = context.WorkflowGraphs.FirstOrDefault(x => x.Workflow.Identity.DefinitionId == consumedWorkflowDefinitionId);

        if (consumedWorkflowGraph == null)
            return;

        var childNode = context.VisitedNodes.FirstOrDefault(x => x.WorkflowGraph == consumedWorkflowGraph);

        if (childNode == null)
        {
            childNode = new WorkflowGraphNode(consumedWorkflowGraph);
            context.VisitedNodes.Add(childNode);
            VisitWorkflowGraph(childNode.WorkflowGraph, context);
        }

        node.Successors.Add(childNode);
        childNode.Predecessors.Add(node);
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

    private class WorkflowGraphNetworkBuilderContext
    {
        public ICollection<WorkflowGraph> WorkflowGraphs { get; set; } = new List<WorkflowGraph>();
        public HashSet<WorkflowGraphNode> VisitedNodes { get; set; } = new();
    }
}