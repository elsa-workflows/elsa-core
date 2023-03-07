using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Services;

public class MemoryUserStore : IUserStore
{
    private readonly MemoryStore<User> _store;

    public MemoryUserStore(MemoryStore<User> store)
    {
        _store = store;
    }
    
    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        _store.Save(user, x => x.Id);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        _store.Delete(id);
        return Task.CompletedTask;
    }

    public Task<User?> FindAsync(string userName, CancellationToken cancellationToken = default)
    {
        var user = _store.Find(x => string.Equals(x.Name, userName, StringComparison.InvariantCultureIgnoreCase));
        return Task.FromResult(user);
    }
}