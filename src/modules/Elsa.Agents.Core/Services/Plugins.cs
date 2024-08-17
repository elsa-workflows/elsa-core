namespace Elsa.Agents;

public class PluginsDiscoverer(IEnumerable<IPluginProvider> providers) : IPluginsDiscoverer
{
    public IEnumerable<PluginDescriptor> GetPluginDescriptors()
    {
        return providers.SelectMany(x => x.GetPlugins());
    }
}