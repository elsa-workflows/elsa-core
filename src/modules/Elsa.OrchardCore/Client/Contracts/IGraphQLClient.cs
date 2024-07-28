namespace Elsa.OrchardCore.Client;

public interface IGraphQLClient
{
    Task<object> SendQueryAsync(string query, Type? targetType, CancellationToken cancellationToken = default);
}