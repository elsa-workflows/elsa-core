using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Models;

/// <summary>
/// Represents a role filter.
/// </summary>
public class RoleFilter
{
    /// <summary>
    /// Gets or sets the role ID to filter for.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the role IDs to filter for.
    /// </summary>
    public ICollection<string>? Ids { get; set; }

    /// <summary>
    /// Gets or sets the role name to filter for. Combined with the ambient tenant that every role store applies,
    /// this finds a role by name within the current tenant, which is how a role should be resolved when only its
    /// name is known: role IDs are unique across the whole store, while names are only unique per tenant.
    /// </summary>
    /// <remarks>
    /// Matching follows the store's own string comparison (for example, the database collation). Use
    /// <see cref="Elsa.Extensions.RoleStoreExtensions.FindByNameAsync"/> for a lookup that also guards against
    /// stores that do not apply this field.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tenant to filter for. The tenant-agnostic sentinel is always included, matching
    /// the Entity Framework query filter, so a shared platform role remains visible from every tenant.
    /// Legacy records without a tenant remain visible to the default tenant for backwards compatibility.
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<Role> Apply(IQueryable<Role> queryable)
    {
        var filter = this;
        if (filter.Id != null)
        {
            queryable = queryable.Where(x => x.Id == filter.Id);
        }

        if (filter.Ids != null)
        {
            queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        }

        if (filter.Name != null)
        {
            queryable = queryable.Where(x => x.Name == filter.Name);
        }

        if (filter.TenantId != null)
        {
            queryable = queryable.Where(x => x.TenantId == filter.TenantId || x.TenantId == Tenant.AgnosticTenantId || (x.TenantId == null && filter.TenantId == Tenant.DefaultTenantId));
        }

        return queryable;
    }
}
