namespace Elsa.Agents;

public class ServiceConfig
{
    public string Name { get; set; }
    public string Type { get; set; }
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
}