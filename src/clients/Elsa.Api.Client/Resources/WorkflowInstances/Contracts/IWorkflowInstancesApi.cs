using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Contracts;

/// <summary>
/// Represents a client for the workflow instances API.
/// </summary>
public interface IWorkflowInstancesApi
{
    /// <summary>
    /// Returns a list of workflow instances.
    /// </summary>
    [Get("/workflow-instances")]
    Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workflow instance.
    /// </summary>
    /// <param name="id">The ID of the workflow instance to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Delete("/workflow-instances/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a list of workflow instances.
    /// </summary>
    /// <param name="request">The request containing the IDs of the workflow instances to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/bulk-actions/delete/workflow-instances/by-id")]
    Task BulkDeleteAsync(BulkDeleteWorkflowInstancesRequest request, CancellationToken cancellationToken);
}