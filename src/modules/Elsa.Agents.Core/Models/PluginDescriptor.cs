using System.ComponentModel;
using System.Reflection;

namespace Elsa.Agents;

/// <summary>
/// A descriptor for a plugin.
/// </summary>
public class PluginDescriptor
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Type PluginType { get; set; }
    
    public PluginDescriptorModel ToModel() => new()
    {
        Name = Name,
        Description = Description,
        PluginType = PluginType.AssemblyQualifiedName!
    };

    public static PluginDescriptor From<TPlugin>(string? name = null)
    {
        var pluginType = typeof(TPlugin);
        var description = pluginType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        return new PluginDescriptor
        {
            Name = name ?? pluginType.Name.Replace("Plugin", ""),
            Description = description,
            PluginType = pluginType
        };
    }
}