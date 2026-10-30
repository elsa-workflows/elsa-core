using Elsa.Sql.Client;

namespace Elsa.Sql.Contracts;

public interface ISqlClientFactory
{
    /// <summary>
    /// Create an instance of the registered client.
    /// </summary>
    /// <param name="clientName">The name of the registered client to create. This can either be clientName used during registration or the default nameof(client) itself.</param>
    /// <param name="connectionString">Connection string.</param>
    /// <returns></returns>
    public ISqlClient CreateClient(string clientName, string connectionString);
}