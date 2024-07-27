namespace Elsa.SemanticKernel;

public class AgentManager
{
    private readonly IDictionary<string, Agent> _agents = new Dictionary<string, Agent>();

    public void AddAgent(Agent agent)
    {
        _agents[agent.Name] = agent;
    }

    public Agent GetAgent(string name)
    {
        return _agents[name];
    }

    public IEnumerable<Agent> GetAgents()
    {
        return _agents.Values;
    }
}