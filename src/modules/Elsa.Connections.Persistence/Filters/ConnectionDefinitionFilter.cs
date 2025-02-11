using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Entities;

namespace Elsa.Connections.Persistence.Filters;
public class ConnectionDefinitionFilter
{
    public string? Id { get; set; }
    public string? Name { get; set; }

    public string? NotId { get; set; }
    public string? Type { get; set; }

    public IQueryable<ConnectionDefinition> Apply(IQueryable<ConnectionDefinition> queryable)
    {
        if(!string.IsNullOrEmpty(Id)) queryable = queryable.Where(x=>x.Id == Id);
        if (!string.IsNullOrWhiteSpace(NotId)) queryable = queryable.Where(x => x.Id != NotId);
        if (!string.IsNullOrWhiteSpace(Name)) queryable = queryable.Where(x=> x.Name == Name);
        if(!string.IsNullOrWhiteSpace(Type)) queryable = queryable.Where(x=>x.ConnectionType == Type);
        return queryable;
    }
}
