namespace Elsa.Agents;

public interface IServiceDiscoverer
{
    IEnumerable<IAgentServiceProvider> Discover();
}