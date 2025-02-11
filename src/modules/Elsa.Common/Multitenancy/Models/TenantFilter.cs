using JetBrains.Annotations;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant filter.
/// </summary>
[UsedImplicitly]
public class TenantFilter
{
    /// <summary>
    /// Gets or sets the tenant ID to filter for.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<Tenant> Apply(IQueryable<Tenant> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);

        return queryable;
    }
    
    public static TenantFilter ById(string id) => new() { Id = id };
}