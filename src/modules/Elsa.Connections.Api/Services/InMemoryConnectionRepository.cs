using Elsa.Common.Services;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Connections.Api.Services;

public class InMemoryConnectionRepository(MemoryStore<ConnectionConfigurationMetadataModel> memoryStore) : IConnectionRepository
{
    public Task AddConnectionConfigurationAsync(ConnectionConfigurationMetadataModel model, CancellationToken cancellationToken = default)
    {
        model.Id = Guid.NewGuid().ToString("n");
        memoryStore.Add(model, model =>model.Id);
        return Task.CompletedTask;
    }

    public Task DeleteConnectionConfigurationAsync(string id, CancellationToken cancellationToken = default)
    {
        memoryStore.Delete(id);
        return Task.CompletedTask;
    }

    public Task<ConnectionConfigurationMetadataModel> GetConnectionAsync(string name, CancellationToken cancellationToken = default)
    {
        var result = memoryStore.Find(c => c.Name == name);
        return Task.FromResult(result);
    }

    public Task<ICollection<ConnectionConfigurationMetadataModel>> GetConnectionsAsync(CancellationToken cancellationToken)
    {
        ICollection<ConnectionConfigurationMetadataModel> results = memoryStore.List().ToList() ;
        return Task.FromResult(results);
    }

    public Task<ICollection<ConnectionConfigurationMetadataModel>> GetConnectionsFromTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        ICollection<ConnectionConfigurationMetadataModel> items = memoryStore.FindMany(c => c.ConnectionType == type).ToList();
        return Task.FromResult(items);
    }

    public Task UpdateConnectionAsync(string id, ConnectionConfigurationMetadataModel model, CancellationToken cancellationToken = default)
    {
        memoryStore.Update(model, model => model.Id);
        return Task.CompletedTask;
    }
}
