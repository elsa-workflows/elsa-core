namespace Elsa.OrchardCore.Client.Contracts;

/// <summary>
/// Defines the contract for a client that communicates with a GraphQL endpoint.
/// </summary>
public interface IGraphQLClient
{
    /// <summary>
    /// Sends a GraphQL query to a configured endpoint and processes the response.
    /// </summary>
    /// <param name="query">The GraphQL query to execute.</param>
    /// <param name="targetType">The target type to which the response will be converted. If null, defaults to JsonObject.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response.</returns>
    Task<object> SendQueryAsync(string query, Type? targetType, CancellationToken cancellationToken = default);
}