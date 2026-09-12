using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Resolves the declared destination of an output binding.
/// </summary>
public interface IOutputBindingDestinationResolver
{
    /// <summary>Resolves a destination from active workflow memory.</summary>
    OutputBindingDestination? Resolve(ActivityExecutionContext activityExecutionContext, Output output);

    /// <summary>Resolves a destination from a materialized workflow graph.</summary>
    OutputBindingDestination? Resolve(WorkflowGraph workflowGraph, ActivityNode activityNode, Output output);
}
