using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.EntityFrameworkCore.Common;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

/// An EF Core implementation of <see cref="IApiKeyStore"/>.
[UsedImplicitly]
public class EFCoreApiKeyStore(EntityStore<AgentsElsaDbContext, ApiKeyDefinition> store) : IApiKeyStore
{
    public Task UpdateAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.UpdateAsync(entity, cancellationToken);
    }

    public Task<ApiKeyDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new ApiKeyDefinitionFilter
        {
            Id = id
        };
        
        return FindAsync(filter, cancellationToken);
    }

    public Task<ApiKeyDefinition?> FindAsync(ApiKeyDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter.Apply, cancellationToken);
    }

    public Task<IEnumerable<ApiKeyDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        return store.ListAsync(cancellationToken);
    }

    public Task AddAsync(ApiKeyDefinition entity, CancellationToken cancellationToken = default)
    {
        return store.AddAsync(entity, cancellationToken);
    }
}