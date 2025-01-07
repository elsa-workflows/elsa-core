using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

public interface IConnectionRepository
{
    public Task<ConnectionConfigurationMetadataModel> GetConnectionAsync(string name, CancellationToken cancellationToken = default);
    public Task<ICollection<ConnectionConfigurationMetadataModel>> GetConnectionsAsync(CancellationToken cancellationToken = default);
    public Task<ICollection<ConnectionConfigurationMetadataModel>> GetConnectionsFromTypeAsync(string type, CancellationToken cancellationToken = default);

    public Task AddConnectionConfigurationAsync(ConnectionConfigurationMetadataModel model, CancellationToken cancellationToken = default);
    
    public Task UpdateConnectionAsync(string name, ConnectionConfigurationMetadataModel model, CancellationToken cancellationToken = default);
    public Task DeleteConnectionConfigurationAsync(string id, CancellationToken cancellationToken = default);
}
