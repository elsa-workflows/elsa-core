namespace Elsa.ResourceManagement.Metadata.Models;

/// <summary>
/// Offers a method for configuring resource type definitions to either display or conceal global settings from appearing on the UI.
/// </summary>
public class ResourceTypeDefinitionOptions
{
    /// <summary>
    /// Configure the driver options for all resource types that share the same Stereotype.
    /// In this dictionary, the 'key' denotes the Stereotype, while the 'value' corresponds to the driver options.
    /// </summary>
    public Dictionary<string, ResourceTypeDefinitionDriverOptions> Stereotypes { get; } = [];

    /// <summary>
    /// Configure the driver options for each resource type.
    /// In this dictionary, the 'key' denotes the resource type, while the 'value' corresponds to the driver options.
    /// </summary>
    public Dictionary<string, ResourceTypeDefinitionDriverOptions> ResourceTypes { get; } = [];
}
