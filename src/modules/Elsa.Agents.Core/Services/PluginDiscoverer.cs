namespace Elsa.Agents;

public class PluginDiscoverer(IEnumerable<IPluginProvider> providers) : IPluginDiscoverer
{
    public IEnumerable<PluginDescriptor> GetPluginDescriptors()
    {
        return providers.SelectMany(x => x.GetPlugins());
    }
}