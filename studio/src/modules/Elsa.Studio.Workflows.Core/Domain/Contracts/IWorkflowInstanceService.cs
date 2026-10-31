using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Defines the contract for workflow instance service.
/// </summary>
public interface IWorkflowInstanceService
{
    Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default);
    Task CancelAsync(string instanceId, CancellationToken cancellationToken = default);
    Task BulkCancelAsync(BulkCancelWorkflowInstancesRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = default, int? skip = default, int? take = default, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportAsync(string id, CancellationToken cancellationToken = default);
    Task<FileDownload> BulkExportAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<int> BulkImportAsync(IEnumerable<StreamPart> streamParts, CancellationToken cancellationToken = default);
    Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string instanceId, CancellationToken cancellationToken = default);
}