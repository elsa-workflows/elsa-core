using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.OrchardCore.Client.Models;

namespace Elsa.OrchardCore.Client.Services;

/// <inheritdoc />
[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class DefaultSecurityTokenClient(HttpClient httpClient) : ISecurityTokenClient
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <inheritdoc />
    public async Task<SecurityToken> GetSecurityTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });
        
        var response = await httpClient.PostAsync("/connect/token", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<SecurityToken>(responseContent, _jsonSerializerOptions)!;
    }
}