using Elsa.Common.Entities;
using JetBrains.Annotations;

namespace Elsa.Common.Filters;

/// <summary>
/// Represents a tenant filter.
/// </summary>
[UsedImplicitly]
public class TenantFilter
{
    /// <summary>
    /// Gets or sets the tenant ID to filter for.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<Tenant> Apply(IQueryable<Tenant> queryable)
    {
        if (TenantId != null) queryable = queryable.Where(x => x.TenantId == TenantId);

        return queryable;
    }
}