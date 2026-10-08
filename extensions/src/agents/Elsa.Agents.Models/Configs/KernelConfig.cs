namespace Elsa.Agents;

public class KernelConfig
{
    public IDictionary<string, AgentConfig> Agents { get; } = new Dictionary<string, AgentConfig>();
}