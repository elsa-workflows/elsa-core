using Elsa.KeyValues.Entities;

namespace Elsa.KeyValues.Models;

public class KeyValueFilter
{
    /// <summary>
    /// Gets or sets whether the <see cref="Key"/> needs to match the beginning of the key found.
    /// </summary>
    public bool StartsWith { get; set; }

    /// <summary>
    /// Gets or sets the key to filter for.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the keys to filter for.
    /// </summary>
    public ICollection<string>? Keys { get; set; }

    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<SerializedKeyValuePair> Apply(IQueryable<SerializedKeyValuePair> queryable)
    {
        var filter = this;
        if (filter.Key != null)
        {
            queryable = StartsWith
                ? queryable.Where(x => x.Id.StartsWith(filter.Key))
                : queryable.Where(x => x.Id == filter.Key);
        }

        if (filter.Keys != null) queryable = queryable.Where(x => filter.Keys.Contains(x.Id));

        return queryable;
    }
}