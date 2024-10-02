using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Models;

public class ResourceTypeDefinition : ResourceDefinition
{
    public ResourceTypeDefinition(string name, string displayName, IEnumerable<ResourceTypePartDefinition> parts, JsonObject settings)
    {
        Name = name;
        DisplayName = displayName;
        Parts = parts.ToList();
        Settings = settings.Clone();

        foreach (var part in Parts)
        {
            part.ResourceTypeDefinition = this;
        }
    }

    public ResourceTypeDefinition(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
        Parts = [];
        Settings = [];
    }

    [Required, StringLength(1024)] public string DisplayName { get; private set; }

    public IEnumerable<ResourceTypePartDefinition> Parts { get; private set; }

    /// <summary>
    /// Returns the <see cref="DisplayName"/> value of the type if defined,
    /// or the <see cref="ResourceDefinition.Name"/> otherwise.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.IsNullOrEmpty(DisplayName)
            ? Name
            : DisplayName;
    }
}