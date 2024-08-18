using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.EntityFrameworkCore.Common;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

/// An EF Core implementation of <see cref="IServiceStore"/>.
[UsedImplicitly]
public class EFCoreServiceStore(EntityStore<AgentsDbContext, ServiceDefinition> store) : IServiceStore
{
    public Task AddAsync(ServiceDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.AddAsync(entity, cancellationToken);
    }
    
    public Task UpdateAsync(ServiceDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.UpdateAsync(entity, cancellationToken);
    }

    public Task<ServiceDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new ServiceDefinitionFilter
        {
            Id = id
        };
        
        return FindAsync(filter, cancellationToken);
    }

    public Task<ServiceDefinition?> FindAsync(ServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter.Apply, cancellationToken);
    }

    public Task<IEnumerable<ServiceDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        return store.ListAsync(cancellationToken);
    }

    public Task DeleteAsync(ServiceDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.DeleteAsync(entity, cancellationToken);
    }
}