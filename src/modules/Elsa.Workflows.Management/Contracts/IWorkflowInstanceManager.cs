using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// A service that manages workflow instances.
/// </summary>
public interface IWorkflowInstanceManager
{
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
}