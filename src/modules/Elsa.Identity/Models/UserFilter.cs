using Elsa.Identity.Entities;

namespace Elsa.Identity.Models;

/// <summary>
/// Represents a user filter.
/// </summary>
public class UserFilter
{
    /// <summary>
    /// Gets or sets the user ID to filter for.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the user name to filter for.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID to filter for.
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<User> Apply(IQueryable<User> queryable)
    {
        var filter = this;
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Name != null) queryable = queryable.Where(x => x.Name == filter.Name);
        if (filter.TenantId != null) queryable = queryable.Where(x => x.TenantId == filter.TenantId);

        return queryable;
    }
}
