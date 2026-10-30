using Elsa.Api.Client.Resources.LogPersistenceStrategies;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class RemoteLogPersistenceStrategyService(IBackendApiClientProvider backendApiClientProvider) : ILogPersistenceStrategyService
{
    private ICollection<LogPersistenceStrategyDescriptor>? _descriptors;
    
    /// <inheritdoc />
    public async Task<IEnumerable<LogPersistenceStrategyDescriptor>> GetLogPersistenceStrategiesAsync(CancellationToken cancellationToken = default)
    {
        if (_descriptors == null)
        {
            var api = await backendApiClientProvider.GetApiAsync<ILogPersistenceStrategiesApi>(cancellationToken);
            var response = await api.ListAsync(cancellationToken);
            _descriptors = response.Items;
        }

        return _descriptors;
    }
}