using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management;

/// <summary>
/// Defines the operations for managing variables associated with a workflow instance.
/// </summary>
public interface IWorkflowInstanceVariableManager
{
    /// <summary>
    /// Retrieves all variables for the specified workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance.</param>
    /// <param name="excludeTags"></param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="ResolvedVariable"/> instances.</returns>
    Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string workflowInstanceId, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all variables for the specified workflow instance.
    /// </summary>
    /// <param name="workflowInstance">The workflow instance from which to retrieve variables.</param>
    /// <param name="excludeTags">A collection of tags to exclude from the variable results, if any.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="ResolvedVariable"/> instances.</returns>
    Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowInstance workflowInstance, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all variables for the specified workflow state.
    /// </summary>
    /// <param name="workflowState">The workflow state to retrieve variables from.</param>
    /// <param name="excludeTags">Optional tags to exclude from the result.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="ResolvedVariable"/> instances.</returns>
    Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowState workflowState, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all variables from the specified <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="workflowExecutionContext">The context of the workflow execution.</param>
    /// <param name="excludeTags"></param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="ResolvedVariable"/> instances.</returns>
    Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default);
    
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