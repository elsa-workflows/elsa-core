using System.Net;
using Elsa.Studio.AI.Client;
using Elsa.Studio.AI.Contracts;
using Elsa.Studio.AI.Models;
using Elsa.Studio.Contracts;
using Refit;

namespace Elsa.Studio.AI.Services;

/// <summary>
/// Loads Weaver data through the active backend API client.
/// </summary>
public class RemoteWeaverService(IBackendApiClientProvider backendApiClientProvider, WeaverStreamClient streamClient) : IWeaverService
{
    public async Task<WeaverCapabilities?> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IWeaverApi>(cancellationToken);
        return await NotFoundAsDefault<WeaverCapabilities?>(async () => await api.GetCapabilitiesAsync(cancellationToken), null);
    }

    public async Task<IReadOnlyCollection<WeaverToolDefinition>> GetToolsAsync(string? agent = default, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IWeaverApi>(cancellationToken);
        return await NotFoundAsDefault(() => api.GetToolsAsync(agent, cancellationToken), []);
    }

    public IAsyncEnumerable<WeaverStreamEvent> StreamChatAsync(WeaverChatRequest request, CancellationToken cancellationToken = default) =>
        streamClient.StreamChatAsync(request, cancellationToken);

    private static async Task<T> NotFoundAsDefault<T>(Func<Task<T>> action, T defaultValue)
    {
        try
        {
            return await action();
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return defaultValue;
        }
        catch (HttpRequestException)
        {
            return defaultValue;
        }
    }
}
