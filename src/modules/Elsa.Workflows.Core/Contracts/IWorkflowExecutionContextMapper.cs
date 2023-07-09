using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Maps a workflow execution context to a workflow state and vice versa.
/// </summary>
public interface IWorkflowExecutionContextMapper
{
    /// <summary>
    /// Maps a workflow execution context to a workflow state.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context to map.</param>
    /// <returns>The mapped workflow state.</returns>
    WorkflowState Extract(WorkflowExecutionContext workflowExecutionContext);
    
    /// <summary>
    /// Maps a workflow state onto a workflow execution context.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context to map onto.</param>
    /// <param name="state">The workflow state to apply.</param>
    /// <returns>The updated workflow execution context.</returns>
    WorkflowExecutionContext Apply(WorkflowExecutionContext workflowExecutionContext, WorkflowState state);
}