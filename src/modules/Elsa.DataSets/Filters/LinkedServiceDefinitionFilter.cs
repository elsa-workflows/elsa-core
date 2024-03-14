using Elsa.DataSets.Entities;

namespace Elsa.DataSets.Filters;

public class LinkedServiceDefinitionFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? Name { get; set; }
    public ICollection<string>? Names { get; set; }
    
    public IQueryable<LinkedServiceDefinition> Apply(IQueryable<LinkedServiceDefinition> query)
    {
        if (Id != null) query = query.Where(x => x.Id == Id);
        if (Ids != null) query = query.Where(x => Ids.Contains(x.Id));
        if (Name != null) query = query.Where(x => x.Name == Name);
        if (Names != null) query = query.Where(x => Names.Contains(x.Name));
        return query;
    }
}