using Elsa.Workflows.State;

namespace Elsa.Workflows;

/// <summary>
/// Extracts workflow state from a workflow execution context and vice versa.
/// </summary>
public interface IWorkflowStateExtractor
{
    /// <summary>
    /// Extracts workflow state from a workflow execution context.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context to map.</param>
    /// <returns>The mapped workflow state.</returns>
    WorkflowState Extract(WorkflowExecutionContext workflowExecutionContext);

    /// <summary>
    /// Applies workflow state to a workflow execution context.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context to map onto.</param>
    /// <param name="state">The workflow state to apply.</param>
    /// <returns>The updated workflow execution context.</returns>
    Task<WorkflowExecutionContext> ApplyAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState state);
}