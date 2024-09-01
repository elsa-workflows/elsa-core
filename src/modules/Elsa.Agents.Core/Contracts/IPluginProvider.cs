namespace Elsa.Agents;

/// <summary>
/// Implementations of this interface are responsible for providing plugins.
/// </summary>
public interface IPluginProvider
{
    IEnumerable<PluginDescriptor> GetPlugins();
}