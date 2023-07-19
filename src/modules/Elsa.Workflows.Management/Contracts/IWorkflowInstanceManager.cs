using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// A service that manages workflow instances.
/// </summary>
public interface IWorkflowInstanceManager
{
    /// <summary>
    /// Deletes all workflow instances that match the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use to select the workflow instances to delete.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of workflow instances that were deleted.</returns>
    Task<long> BulkDeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
}