using Elsa.Identity.Entities;
using Elsa.Identity.Services;
using Elsa.Persistence.Common.Implementations;

namespace Elsa.Identity.Implementations;

public class MemoryRoleStore : IRoleStore
{
    private readonly MemoryStore<Role> _store;

    public MemoryRoleStore(MemoryStore<Role> store)
    {
        _store = store;
    }
    
    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        _store.Save(role);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        _store.Delete(id);
        return Task.CompletedTask;
    }
}