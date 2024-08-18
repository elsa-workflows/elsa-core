namespace Elsa.Agents;

public interface IPluginDiscoverer
{
    IEnumerable<PluginDescriptor> GetPluginDescriptors();
}