using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management;

/// <summary>
/// A service that manages workflow instances.
/// </summary>
public interface IWorkflowInstanceManager
{
    /// <summary>
    /// Finds the workflow instance with the specified ID.
    /// </summary>
    Task<WorkflowInstance?> FindByIdAsync(string instanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the first workflow instance that matches the specified filter.
    /// </summary>
    Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a workflow instance with the specified ID exists.
    /// </summary>
    Task<bool> ExistsAsync(string instanceId, CancellationToken cancellationToken = default);

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
    /// Saves the specified workflow instance.
    /// </summary>
    Task CreateAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps the specified workflow state to a workflow instance and saves it.
    /// </summary>
    /// <param name="workflowState">The workflow state to map to a workflow instance.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The workflow instance that was saved.</returns>
    Task<WorkflowInstance> CreateAsync(WorkflowState workflowState, CancellationToken cancellationToken);

    /// <summary>
    /// Extracts the workflow state from the specified workflow execution context, maps it to a workflow instance and saves it.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context to extract the workflow state from.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The workflow instance that was saved.</returns>
    Task<WorkflowInstance> CreateAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the specified workflow instance.
    /// </summary>
    Task UpdateAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Maps the specified workflow state to a workflow instance and updates it.
    /// </summary>
    /// <param name="workflowState">The workflow state to map to a workflow instance.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The workflow instance that was saved.</returns>
    Task<WorkflowInstance> UpdateAsync(WorkflowState workflowState, CancellationToken cancellationToken);

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
    string SerializeWorkflowState(WorkflowState workflowState);
    
    /// <summary>
    /// Instantiates and saves a new workflow instance.
    /// </summary>
    Task<WorkflowInstance> CreateAndCommitWorkflowInstanceAsync(Workflow workflow, WorkflowInstanceOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Instantiates a new workflow instance.
    /// </summary>
    WorkflowInstance CreateWorkflowInstance(Workflow workflow, WorkflowInstanceOptions? options = null);
}