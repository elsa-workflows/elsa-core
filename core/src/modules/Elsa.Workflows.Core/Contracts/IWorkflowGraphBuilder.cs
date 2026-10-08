using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Builds a workflow graph from a workflow.
/// </summary>
public interface IWorkflowGraphBuilder
{
    /// <summary>
    /// Builds a workflow graph from a workflow.
    /// </summary>
    Task<WorkflowGraph> BuildAsync(Workflow workflow, CancellationToken cancellationToken = default);
}