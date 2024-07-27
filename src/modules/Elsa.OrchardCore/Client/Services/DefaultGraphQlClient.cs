using System.Net.Http.Json;
using System.Text.Json;

namespace Elsa.OrchardCore.Client;

public class DefaultGraphQlClient(HttpClient httpClient) : IGraphQLClient
{
    public async Task<JsonElement> SendQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("api/graphql", content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }
}