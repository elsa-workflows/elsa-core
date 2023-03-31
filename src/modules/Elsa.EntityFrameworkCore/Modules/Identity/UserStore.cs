using Elsa.EntityFrameworkCore.Common;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.EntityFrameworkCore.Modules.Identity;

/// <summary>
/// An EF Core implementation of <see cref="IUserStore"/>.
/// </summary>
public class EFCoreUserStore : IUserStore
{
    private readonly EntityStore<IdentityElsaDbContext, User> _userStore;

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreUserStore"/>.
    /// </summary>
    public EFCoreUserStore(EntityStore<IdentityElsaDbContext, User> userStore)
    {
        _userStore = userStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        await _userStore.SaveAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        await _userStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        return await _userStore.FindAsync(query => Filter(query, filter), cancellationToken);
    }
    
    private static IQueryable<User> Filter(IQueryable<User> query, UserFilter filter) => filter.Apply(query);
}