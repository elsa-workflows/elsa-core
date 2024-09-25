namespace Elsa.Agents;

public class ServiceDiscoverer(IEnumerable<IAgentServiceProvider> providers) : IServiceDiscoverer
{
    public IEnumerable<IAgentServiceProvider> Discover()
    {
        return providers;
    }
}