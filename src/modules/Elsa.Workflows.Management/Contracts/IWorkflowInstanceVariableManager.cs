namespace Elsa.Workflows.Management;

public interface IWorkflowInstanceVariableManager
{
    /// <summary>
    /// Retrieves all variables for the specified workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance.</param>
    /// <param name="excludeTags"></param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="ResolvedVariable"/> instances.</returns>
    Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string workflowInstanceId, IEnumerable<string>? excludeTags = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all variables from the specified <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="workflowExecutionContext">The context of the workflow execution.</param>
    /// <param name="excludeTags"></param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="ResolvedVariable"/> instances.</returns>
    Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<string>? excludeTags = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the specified variables in the specified workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance.</param>
    /// <param name="variables">The collection of variables to set.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A complete list of resolved variables.</returns>
    Task<IEnumerable<ResolvedVariable>> SetVariablesAsync(string workflowInstanceId, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the specified variables in the given <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="workflowExecutionContext">The context of the workflow execution.</param>
    /// <param name="variables">The collection of variables to set.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A complete list of resolved variables.</returns>
    Task<IEnumerable<ResolvedVariable>> SetVariablesAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default);
}