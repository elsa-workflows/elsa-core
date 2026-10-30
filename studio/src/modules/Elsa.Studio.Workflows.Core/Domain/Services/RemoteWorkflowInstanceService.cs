using System.Net;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow instance service that uses a remote backend to retrieve workflow instances.
/// </summary>
public class RemoteWorkflowInstanceService : IWorkflowInstanceService
{
    private readonly IBackendApiClientProvider _backendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowInstanceService"/> class.
    /// </summary>
    public RemoteWorkflowInstanceService(IBackendApiClientProvider backendApiClientProvider)
    {
        _backendApiClientProvider = backendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.ListAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await api.DeleteAsync(instanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default)
    {
        var request = new BulkDeleteWorkflowInstancesRequest
        {
            Ids = instanceIds.ToList()
        };
        var api = await GetApiAsync(cancellationToken);
        await api.BulkDeleteAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await api.CancelAsync(instanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task BulkCancelAsync(BulkCancelWorkflowInstancesRequest request, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await api.BulkCancelAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await GetApiAsync(cancellationToken);
            return await api.GetAsync(id, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var request = new GetFilteredJournalRequest
        {
            Filter = filter
        };
        var api = await GetApiAsync(cancellationToken);
        return await api.GetFilteredJournalAsync(instanceId, request, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileDownload> ExportAsync(string id, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.ExportAsync(id, cancellationToken);
        var fileName = response.GetDownloadedFileNameOrDefault($"workflow-instance-{id}.json");

        return new(fileName, response.Content!);
    }

    /// <inheritdoc />
    public async Task<FileDownload> BulkExportAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var request = new BulkExportWorkflowInstancesRequest(ids.ToArray());
        var response = await api.BulkExportAsync(request, cancellationToken);
        var fileName = response.GetDownloadedFileNameOrDefault("workflow-instances.zip");
        return new(fileName, response.Content!);
    }

    /// <inheritdoc />
    public async Task<int> BulkImportAsync(IEnumerable<StreamPart> streamParts, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkImportAsync(streamParts.ToList(), cancellationToken);
        return response.Count;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.GetVariablesAsync(instanceId, cancellationToken);
        return response.Items;
    }

    private async Task<IWorkflowInstancesApi> GetApiAsync(CancellationToken cancellationToken = default) => await _backendApiClientProvider.GetApiAsync<IWorkflowInstancesApi>(cancellationToken);
}