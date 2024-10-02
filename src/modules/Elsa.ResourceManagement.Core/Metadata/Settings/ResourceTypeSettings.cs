namespace Elsa.ResourceManagement.Metadata.Settings;

public class ResourceTypeSettings
{
    /// <summary>
    /// Used to determine if an instance of this resource type can be created through the UI.
    /// </summary>
    public bool Creatable { get; set; }

    /// <summary>
    /// Used to determine if an instance of this resource type can be listed in the resources page.
    /// </summary>
    public bool Listable { get; set; }

    /// <summary>
    /// Used to determine if this resource type supports draft versions.
    /// </summary>
    public bool Draftable { get; set; }

    /// <summary>
    /// Used to determine if this resource type supports versioning.
    /// </summary>
    public bool Versionable { get; set; }

    /// <summary>
    /// Defines the stereotype of the type.
    /// </summary>
    public string Stereotype { get; set; }

    /// <summary>
    /// Used to determine if this resource type supports custom permissions.
    /// </summary>
    public bool Securable { get; set; }

    /// <summary>
    /// Gets or sets the description name of this resource type.
    /// </summary>
    public string Description { get; set; }
}