using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.OrchardCore.Client;

public class DefaultRestApiClient(HttpClient httpClient) : IRestApiClient
{
    public async Task<JsonNode> GetContentItemAsync(string contentItemId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/content/{contentItemId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
    }

    public async Task<JsonNode> PatchContentItemAsync(string contentItemId, PatchContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(request);
        var response = await httpClient.PatchAsync($"api/content/{contentItemId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
    }

    public async Task<JsonNode> LocalizeContentItemAsync(string contentItemId, LocalizeContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(request);
        var response = await httpClient.PostAsync($"api/content/{contentItemId}/localize", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JsonObject>(json);
    }
}