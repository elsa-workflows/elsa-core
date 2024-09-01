namespace Elsa.Agents;

public abstract class PluginProvider : IPluginProvider
{
    public virtual IEnumerable<PluginDescriptor> GetPlugins() => [];
}