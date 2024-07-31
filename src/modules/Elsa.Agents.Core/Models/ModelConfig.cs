namespace Elsa.Agents;

public class ModelConfig
{
    public string Name { get; set; }
    public ICollection<ServiceConfig> Services { get; set; }
}
