using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class WorkflowGraphBuilder(IActivityVisitor activityVisitor, IIdentityGraphService identityGraphService) : IWorkflowGraphBuilder
{
    /// <inheritdoc />
    public async Task<WorkflowGraph> BuildAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var graph = await activityVisitor.VisitAsync(workflow, cancellationToken);
        var nodes = graph.Flatten().ToList();
        
        identityGraphService.AssignIdentities(nodes);
        return new WorkflowGraph(workflow, graph, nodes);
    }
}