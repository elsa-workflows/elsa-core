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
    /// Returns a workflow instance.
    /// </summary>
    /// <param name="id">The ID of the workflow instance to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/workflow-instances/{id}")]
    Task<WorkflowInstance> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of journal records for the specified workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to return the journal.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/workflow-instances/{workflowInstanceId}/journal")]
    Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string workflowInstanceId, int? skip = default, int? take = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of journal records for the specified workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to return the journal.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/workflow-instances/{workflowInstanceId}/journal")]
    Task<PagedListResponse<WorkflowExecutionLogRecord>> GetFilteredJournalAsync(string workflowInstanceId, GetFilteredJournalRequest? filter, int? skip = default, int? take = default, CancellationToken cancellationToken = default);

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