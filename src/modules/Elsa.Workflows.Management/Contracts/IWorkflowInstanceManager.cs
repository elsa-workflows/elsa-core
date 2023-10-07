using Elsa.Workflows.Core;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// A service that manages workflow instances.
/// </summary>
public interface IWorkflowInstanceManager
{
    /// <summary>
    /// Saves the specified workflow instance.
    /// </summary>
    Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps the specified workflow state to a workflow instance and saves it.
    /// </summary>
    /// <param name="workflowState">The workflow state to map to a workflow instance.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The workflow instance that was saved.</returns>
    Task<WorkflowInstance> SaveAsync(WorkflowState workflowState, CancellationToken cancellationToken);

    /// <summary>
    /// Extracts the workflow state from the specified workflow execution context, maps it to a workflow instance and saves it.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context to extract the workflow state from.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The workflow instance that was saved.</returns>
    Task<WorkflowInstance> SaveAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the first workflow instance that matches the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use to select the workflow instance to delete.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>True if a workflow instance was deleted, otherwise false.</returns>
    Task<bool> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all workflow instances that match the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use to select the workflow instances to delete.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of workflow instances that were deleted.</returns>
    Task<long> BulkDeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extracts the workflow state from the specified workflow execution context.
    /// </summary>
    WorkflowState ExtractWorkflowState(WorkflowExecutionContext workflowExecutionContext);

    /// <summary>
    /// Serializes the specified workflow state.
    /// </summary>
    Task<string> SerializeWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
}