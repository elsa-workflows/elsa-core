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
    /// unrelated role.
    /// </remarks>
    /// <param name="roleStore">The role store.</param>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching role, or <see langword="null"/> when the current tenant has no role with that name.</returns>
    public static async Task<Role?> FindByNameAsync(this IRoleStore roleStore, string name, CancellationToken cancellationToken = default)
    {
        var candidates = (await roleStore.FindManyAsync(new RoleFilter { Name = name }, cancellationToken)).ToList();

        return candidates.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal))
               ?? candidates.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
