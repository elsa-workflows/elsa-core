namespace Elsa.Agents;

public class ServiceProfileConfig
{
    public string Name { get; set; }
    public ICollection<ServiceConfig> Services { get; set; }
}