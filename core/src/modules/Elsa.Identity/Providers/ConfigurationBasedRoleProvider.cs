using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents a provider of roles that uses <see cref="RolesOptions"/>.
/// </summary>
[PublicAPI]
public class ConfigurationBasedRoleProvider : IRoleProvider
{
    private readonly IOptions<RolesOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBasedRoleProvider"/> class.
    /// </summary>
    public ConfigurationBasedRoleProvider(IOptions<RolesOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var rolesQueryable = _options.Value.Roles.AsQueryable();
        var roles = filter.Apply(rolesQueryable).ToList();
        return new(roles);
    }
}