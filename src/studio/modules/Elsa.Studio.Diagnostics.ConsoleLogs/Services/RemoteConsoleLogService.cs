using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Client;
using Elsa.Studio.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Refit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Loads diagnostics console logs through the active backend API client.
/// </summary>
public class RemoteConsoleLogService(IBackendApiClientProvider backendApiClientProvider) : IConsoleLogService
{
    /// <inheritdoc />
    public async Task<RecentConsoleLinesResult> GetRecentAsync(ConsoleLogFilter filter, int rowCap, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IConsoleLogsApi>(cancellationToken);
        var request = ConsoleLogFilterMapper.ToRecentRequest(filter, rowCap);

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
    public async Task<ICollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IConsoleLogsApi>(cancellationToken);

        try
        {
            return await api.ListSourcesAsync(cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return Array.Empty<ConsoleLogSource>();
        }
    }
}
