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
    /// Gets or sets the tenant to filter for. The tenant-agnostic sentinel is always included, matching
    /// the Entity Framework query filter, so a shared platform role remains visible from every tenant.
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
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        if (filter.TenantId != null) queryable = queryable.Where(x => x.TenantId == filter.TenantId || x.TenantId == Tenant.AgnosticTenantId);

        return queryable;
    }
}