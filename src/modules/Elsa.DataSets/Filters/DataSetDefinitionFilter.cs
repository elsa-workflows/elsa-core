using Elsa.DataSets.Entities;

namespace Elsa.DataSets.Filters;

/// <summary>
/// Filter for <see cref="DataSetDefinition"/>.
/// </summary>
public class DataSetDefinitionFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? Name { get; set; }
    public ICollection<string>? Names { get; set; }

    /// <summary>
    /// Applies the filter to the query.
    /// </summary>
    public IQueryable<DataSetDefinition> Apply(IQueryable<DataSetDefinition> query)
    {
        if (Id != null) query = query.Where(x => x.Id == Id);
        if (Ids != null) query = query.Where(x => Ids.Contains(x.Id));
        if (Name != null) query = query.Where(x => x.Name == Name);
        if (Names != null) query = query.Where(x => Names.Contains(x.Name));
        return query;
    }
}