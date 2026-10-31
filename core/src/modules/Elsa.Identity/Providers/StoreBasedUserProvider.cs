using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using JetBrains.Annotations;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents a user provider that finds users using <see cref="IUserStore"/>.
/// </summary>
[PublicAPI]
public class StoreBasedUserProvider : IUserProvider
{
    private readonly IUserStore _userStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreBasedUserProvider"/> class.
    /// </summary>
    public StoreBasedUserProvider(IUserStore userStore)
    {
        _userStore = userStore;
    }
    
    /// <inheritdoc />
    public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        return await _userStore.FindAsync(filter, cancellationToken);
    }
}