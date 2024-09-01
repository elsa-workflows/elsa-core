namespace Elsa.Agents;

public class AgentsOptions
{
    public ICollection<ApiKeyConfig> ApiKeys { get; set; }
    public ICollection<ServiceConfig> Services { get; set; }
    public ICollection<AgentConfig> Agents { get; set; }
}