using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Responses;
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
    [Post("/workflow-instances")]
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
    /// Returns the last-updated timestamp of the specified workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to return its last-updated timestamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns a response containing the last-updated timestamp.</returns>
    [Get("/workflow-instances/{workflowInstanceId}/updated-at")]
    Task<GetUpdatedAtResponse> GetUpdatedAtAsync(string workflowInstanceId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Cancels a workflow instance.
    /// </summary>
    /// <param name="id">The ID of the workflow instance to cancel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/cancel/workflow-instances/{id}")]
    Task CancelAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels a list of workflow instances.
    /// </summary>
    /// <param name="request">The request containing the selection of workflow instances to cancel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/bulk-actions/cancel/workflow-instances")]
    Task BulkCancelAsync(BulkCancelWorkflowInstancesRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// Exports a workflow instance.
    /// </summary>
    /// <param name="id">The ID of the workflow instance to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/workflow-instances/{id}/export")]
    Task<IApiResponse<Stream>> ExportAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a set of workflow instances.
    /// </summary>
    /// <param name="request">The request containing the IDs of the workflow instances to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/bulk-actions/export/workflow-instances")]
    Task<IApiResponse<Stream>> BulkExportAsync(BulkExportWorkflowInstancesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a set of workflow instances.
    /// </summary>
    /// <param name="files">The files to import.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/bulk-actions/import/workflow-instances")]
    [Multipart]
    Task<ImportFilesResponse> BulkImportAsync([AliasAs("files")] List<StreamPart> files, CancellationToken cancellationToken = default);
}