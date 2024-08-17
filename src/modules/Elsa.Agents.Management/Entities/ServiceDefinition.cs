using Elsa.Common.Entities;

namespace Elsa.Agents.Management;

public class ServiceDefinition : Entity
{
    public string Name { get; set; }
    public string Type { get; set; }
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
}