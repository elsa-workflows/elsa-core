using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents a role provider that always returns a single admin role. This is useful for development purposes.
/// </summary>
public class AdminRoleProvider : IRoleProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var adminRole = new Role
        {
            Id = "admin",
            Name = "admin",
            Permissions = { "*" }
        };

        return new(new[] { adminRole });
    }
}