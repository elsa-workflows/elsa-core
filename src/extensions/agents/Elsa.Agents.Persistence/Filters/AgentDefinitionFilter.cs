using Elsa.Agents.Persistence.Entities;

namespace Elsa.Agents.Persistence.Filters;

public class AgentDefinitionFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? NotId { get; set; }
    public string? Name { get; set; }
    
    public IQueryable<AgentDefinition> Apply(IQueryable<AgentDefinition> queryable)
    {
        if (!string.IsNullOrWhiteSpace(Id)) queryable = queryable.Where(x => x.Id == Id);
        if (Ids != null && Ids.Count != 0) queryable = queryable.Where(x => Ids.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(NotId)) queryable = queryable.Where(x => x.Id != NotId);
        if (!string.IsNullOrWhiteSpace(Name)) queryable = queryable.Where(x => x.Name == Name);
        return queryable;
    }
}