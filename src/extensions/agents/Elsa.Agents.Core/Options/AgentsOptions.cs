namespace Elsa.Agents;

public class AgentsOptions
{
    public ICollection<AgentConfig> Agents { get; set; } = new List<AgentConfig>();
    public ICollection<ServiceDescriptor> ServiceDescriptors { get; set; } = new List<ServiceDescriptor>();
}