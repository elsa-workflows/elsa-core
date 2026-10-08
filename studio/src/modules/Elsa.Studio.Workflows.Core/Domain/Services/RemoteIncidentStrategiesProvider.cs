using Elsa.Api.Client.Resources.IncidentStrategies.Contracts;
using Elsa.Api.Client.Resources.IncidentStrategies.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// Provides incident strategies from a remote server.
/// </summary>
public class RemoteIncidentStrategiesProvider(IBackendApiClientProvider backendApiClientProvider) : IIncidentStrategiesProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<IncidentStrategyDescriptor>> GetIncidentStrategiesAsync(CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IIncidentStrategiesApi>(cancellationToken);
        var response = await api.ListAsync(cancellationToken);

        return response.Items;
    }
}