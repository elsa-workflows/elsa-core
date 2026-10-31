using Elsa.Sql.Client;
using Elsa.Sql.Contracts;
using Elsa.Sql.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Sql.Factory;

/// <summary>
/// SQL client factory
/// </summary>
public class SqlClientFactory : ISqlClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SqlClientFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public ISqlClient CreateClient(string clientName, string connectionString)
    {
        if (string.IsNullOrEmpty(clientName))
        {
            throw new ArgumentException($"Client name can not be empty or null.", nameof(clientName));
        }
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException($"Connection string can not be empty or null.", nameof(connectionString));
        }
        if (_serviceProvider.GetRequiredService<ClientStore>().Clients.TryGetValue(clientName, out var clientType))
        {
            try
            {
                return ActivatorUtilities.CreateInstance(_serviceProvider, clientType, connectionString) as ISqlClient;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to create instance of '{clientName}' of type '{clientType}'.", ex);
            }
        }
        throw new ArgumentException($"No registered SQL client provider for '{clientName}'.");
    }
}