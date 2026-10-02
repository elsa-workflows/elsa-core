using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Http;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.OrchardCore.Client.Models;

namespace Elsa.OrchardCore.Client.Services;

/// <inheritdoc />
[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class DefaultRestApiClient(HttpClient httpClient) : IRestApiClient
{
    /// <inheritdoc />
    public async Task<JsonObject> GetContentItemAsync(string contentItemId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/content/{contentItemId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<JsonObject> PatchContentItemAsync(string contentItemId, PatchContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(request);
        var response = await httpClient.PatchAsync($"api/content-items/{contentItemId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<JsonObject> LocalizeContentItemAsync(string contentItemId, LocalizeContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(request);
        var response = await httpClient.PostAsync($"api/content-items/{contentItemId}/localize", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JsonObject>(json)!;
    }

    /// <inheritdoc />
    public async Task<JsonObject> CreateContentItemAsync(CreateContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(request);
        var response = await httpClient.PostAsync("api/content-items", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<JsonObject> UploadFilesAsync(IEnumerable<HttpFile> files, string? folderPath = null, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();

        foreach (var file in files)
        {
            var streamContent = file.GetStreamContent();
            content.Add(streamContent);
        }

        var response = await httpClient.PostAsync($"api/media/upload?path={folderPath}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<JsonObject> ResolveTagsAsync(ResolveTagsRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(request);
        var response = await httpClient.PostAsync("api/tags/resolve", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken))!;
    }
}