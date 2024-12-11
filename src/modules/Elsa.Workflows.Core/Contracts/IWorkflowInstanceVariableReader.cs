namespace Elsa.Workflows;

/// <summary>
/// Enumerates variables for a workflow instance.
/// </summary>
public interface IWorkflowInstanceVariableReader
{
    /// <summary>
    /// Retrieves all variables from the specified <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="workflowExecutionContext">The context of the workflow execution.</param>
    /// <param name="excludeTags"></param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="ResolvedVariable"/> instances.</returns>
    Task<IEnumerable<ResolvedVariable>> GetVariables(WorkflowExecutionContext workflowExecutionContext, IEnumerable<string>? excludeTags = default, CancellationToken cancellationToken = default);
}