using System.Text.Json.Nodes;
using Elsa.Http.Models;

namespace Elsa.OrchardCore.Client;

public interface IRestApiClient
{
    Task<JsonObject> GetContentItemAsync(string contentItemId, CancellationToken cancellationToken = default);
    Task<JsonObject> PatchContentItemAsync(string contentItemId, PatchContentItemRequest request, CancellationToken cancellationToken = default);
    Task<JsonObject> LocalizeContentItemAsync(string contentItemId, LocalizeContentItemRequest request, CancellationToken cancellationToken = default);
    Task<JsonObject> CreateContentItemAsync(CreateContentItemRequest request, CancellationToken cancellationToken = default);
    Task<JsonObject> UploadFilesAsync(IEnumerable<HttpFile> files, string? folderPath = null, CancellationToken cancellationToken = default);
}