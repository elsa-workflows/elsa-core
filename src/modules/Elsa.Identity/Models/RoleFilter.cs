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
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<Role> Apply(IQueryable<Role> queryable)
    {
        var filter = this;
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));

        return queryable;
    }
}