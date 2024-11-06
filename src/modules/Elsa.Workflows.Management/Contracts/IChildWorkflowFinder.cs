using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Finds child workflows for a given workflow graph.
/// </summary>
public interface IChildWorkflowFinder
{
    /// <summary>
    /// Finds child workflows for a given workflow graph.
    /// </summary>
    /// <param name="workflowGraph">The workflow graph.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of child workflows.</returns>
    Task<IEnumerable<WorkflowGraph>> FindChildWorkflowsAsync(WorkflowGraph workflowGraph, CancellationToken cancellationToken = default);
}