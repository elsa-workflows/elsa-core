using System.Text.Json;

namespace Elsa.OrchardCore.Client;

public interface IGraphQLClient
{
    Task<JsonElement> SendQueryAsync(string query, CancellationToken cancellationToken = default);
}