using Elsa.Common.Entities;

namespace Elsa.Agents.Persistence.Entities;

public class ServiceDefinition : Entity
{
    public string Name { get; set; }
    public string Type { get; set; }
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

    public ServiceConfig ToServiceConfig()
    {
        return new ServiceConfig
        {
            Name = Name,
            Type = Type,
            Settings = Settings
        };
    }
    
    public ServiceModel ToModel()
    {
        return new ServiceModel
        {
            Id = Id,
            Name = Name,
            Type = Type,
            Settings = Settings
        };
    }
}