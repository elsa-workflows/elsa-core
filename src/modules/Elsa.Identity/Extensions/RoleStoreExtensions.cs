using Elsa.Common.Multitenancy;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for <see cref="IRoleStore"/>.
/// </summary>
public static class RoleStoreExtensions
{
    /// <summary>
    /// Finds the role with the specified name in the current tenant.
    /// </summary>
    /// <remarks>
    /// Role IDs are unique across the whole store, but role names are only unique per tenant, so a role known
    /// only by its name must be looked up by name within the ambient tenant rather than by an ID derived from the
    /// name. The store applies the ambient tenant. The candidates are matched again here, preferring an exact match
    /// over a case-insensitive one, so a store that ignores <see cref="RoleFilter.Name"/> cannot hand back an
    /// unrelated role, and a role whose name differs only in case is found even on a case-sensitive store. When the
    /// tenant's own role and a tenant-agnostic role share the name, even in a different case, the tenant's own role is
    /// returned.
    /// </remarks>
    /// <param name="roleStore">The role store.</param>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching role, or <see langword="null"/> when the current tenant has no role with that name.</returns>
    public static Task<Role?> FindByNameAsync(this IRoleStore roleStore, string name, CancellationToken cancellationToken = default) =>
        roleStore.FindByNameAsync(name, includeTenantAgnostic: true, cancellationToken);

    /// <summary>
    /// Finds the role with the specified name in the current tenant, optionally ignoring tenant-agnostic roles.
    /// </summary>
    /// <param name="roleStore">The role store.</param>
    /// <param name="name">The role name.</param>
    /// <param name="includeTenantAgnostic">
    /// Whether a tenant-agnostic role (<see cref="Tenant.AgnosticTenantId"/>), which every tenant can see, may match.
    /// Pass <see langword="false"/> when the caller is about to treat the role as its own tenant's, for example to
    /// add permissions to it.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching role, or <see langword="null"/> when there is none.</returns>
    public static async Task<Role?> FindByNameAsync(this IRoleStore roleStore, string name, bool includeTenantAgnostic, CancellationToken cancellationToken = default)
    {
        var nameMatch = BestMatch(await roleStore.FindManyAsync(new RoleFilter { Name = name }, cancellationToken), name, includeTenantAgnostic);

        if (nameMatch is not null && !IsTenantAgnostic(nameMatch))
        {
            return nameMatch;
        }

        // The name filter follows the store's own comparison, which is exact for the in-memory store and SQLite. A
        // name that differs only in case is still the same role to RoleManager, so scan the tenant's roles before
        // settling for a tenant-agnostic match or nothing.
        var tenantMatch = BestMatch(await roleStore.FindManyAsync(new RoleFilter(), cancellationToken), name, includeTenantAgnostic);
        return tenantMatch ?? nameMatch;
    }

    /// <summary>
    /// Several visible roles can match one name: the tenant's own role and a tenant-agnostic one, or roles whose names
    /// differ only in case. The tenant's own role wins over a shared one, then an exact match wins over a
    /// case-insensitive one, and remaining ties are broken by ID, so the result never depends on the order in which
    /// the store returns rows.
    /// </summary>
    private static Role? BestMatch(IEnumerable<Role> roles, string name, bool includeTenantAgnostic) =>
        roles
            .Where(x => includeTenantAgnostic || !IsTenantAgnostic(x))
            .Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(IsTenantAgnostic)
            .ThenBy(x => !string.Equals(x.Name, name, StringComparison.Ordinal))
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .FirstOrDefault();

    internal static bool IsTenantAgnostic(Role role) => role.TenantId == Tenant.AgnosticTenantId;
}
