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
    /// unrelated role, and a role whose name differs only in case is found even on a case-sensitive store.
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
        var candidates = (await roleStore.FindManyAsync(new RoleFilter { Name = name }, cancellationToken))
            .Where(x => includeTenantAgnostic || !IsTenantAgnostic(x))
            .ToList();

        if (candidates.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal)) is { } exactMatch)
        {
            return exactMatch;
        }

        // The name filter follows the store's own comparison, which is exact for the in-memory store and SQLite. A
        // name that differs only in case is still the same role to RoleManager, so fall back to the tenant's roles.
        var tenantRoles = await roleStore.FindManyAsync(new RoleFilter(), cancellationToken);
        return tenantRoles
            .Where(x => includeTenantAgnostic || !IsTenantAgnostic(x))
            .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsTenantAgnostic(Role role) => role.TenantId == Tenant.AgnosticTenantId;
}
