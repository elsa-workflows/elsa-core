namespace Elsa.Agents;

public interface IPluginsDiscoverer
{
    IEnumerable<PluginDescriptor> GetPluginDescriptors();
}