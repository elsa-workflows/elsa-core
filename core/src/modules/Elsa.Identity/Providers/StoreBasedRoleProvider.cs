using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using JetBrains.Annotations;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents a provider of roles that uses <see cref="IRoleStore"/>.
/// </summary>
[PublicAPI]
public class StoreBasedRoleProvider : IRoleProvider
{
    private readonly IRoleStore _roleStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreBasedRoleProvider"/> class.
    /// </summary>
    public StoreBasedRoleProvider(IRoleStore roleStore)
    {
        _roleStore = roleStore;
    }
    
    /// <inheritdoc />
    public async ValueTask<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        return await _roleStore.FindManyAsync(filter, cancellationToken);
    }
}