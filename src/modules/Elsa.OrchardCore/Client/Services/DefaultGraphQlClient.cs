using System.Text.Json.Nodes;
using Elsa.Expressions.Helpers;

namespace Elsa.OrchardCore.Client;

public class DefaultGraphQlClient(HttpClient httpClient) : IGraphQLClient
{
    public async Task<object> SendQueryAsync(string query, Type? targetType = null, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(query, System.Text.Encoding.UTF8, "application/graphql");
        var response = await httpClient.PostAsync("api/graphql", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        targetType ??= typeof(JsonObject);
        return ObjectConverter.ConvertTo(json, targetType);
    }
}