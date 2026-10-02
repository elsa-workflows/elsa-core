using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Client;
using Elsa.Studio.Diagnostics.StructuredLogs.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using Refit;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Services;

/// <summary>
/// Loads structured logs through the active backend API client.
/// </summary>
public class RemoteStructuredLogService(IBackendApiClientProvider backendApiClientProvider) : IStructuredLogService
{
    /// <inheritdoc />
    public async Task<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, int rowCap, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IStructuredLogsApi>(cancellationToken);
        var request = StructuredLogFilterMapper.ToRecentRequest(filter, rowCap);

        try
        {
            return await api.GetRecentAsync(request, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return new();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IStructuredLogsApi>(cancellationToken);

        try
        {
            return await api.ListSourcesAsync(cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return Array.Empty<StructuredLogSource>();
        }
    }

    /// <inheritdoc />
    public async Task<StructuredLogStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IStructuredLogsApi>(cancellationToken);

        try
        {
            return await api.GetStorageDiagnosticsAsync(cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return new();
        }
    }
}
