namespace Elsa.Agents;

public interface IPluginProvider
{
    IEnumerable<PluginDescriptor> GetPlugins();
}