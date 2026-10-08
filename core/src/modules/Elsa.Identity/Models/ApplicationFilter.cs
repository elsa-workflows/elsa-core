using Elsa.Identity.Entities;

namespace Elsa.Identity.Models;

/// <summary>
/// Represents an application filter.
/// </summary>
public class ApplicationFilter
{
    /// <summary>
    /// Gets or sets the application ID to filter for.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the application short ID to filter for.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the application name to filter for.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<Application> Apply(IQueryable<Application> queryable)
    {
        var filter = this;
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.ClientId != null) queryable = queryable.Where(x => x.ClientId == filter.ClientId);
        if (filter.Name != null) queryable = queryable.Where(x => x.Name == filter.Name);

        return queryable;
    }
}