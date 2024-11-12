using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace Elsa.Aspects.Metadata;

public class ResourceTypeDefinition : ResourceDefinition
{
    public ResourceTypeDefinition(string name, string displayName, IEnumerable<ResourceTypeAspectDefinition> aspects, JsonObject settings)
    {
        Name = name;
        DisplayName = displayName;
        Aspects = aspects.ToList();
        Settings = settings.Clone();

        foreach (var aspect in Aspects)
        {
            aspect.ResourceTypeDefinition = this;
        }
    }

    public ResourceTypeDefinition(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
        Aspects = [];
        Settings = [];
    }

    [Required, StringLength(1024)] public string DisplayName { get; private set; }

    public IEnumerable<ResourceTypeAspectDefinition> Aspects { get; private set; }

    /// <summary>
    /// Returns the <see cref="DisplayName"/> value of the type if defined,
    /// or the <see cref="ResourceTypeDefinition.Name"/> otherwise.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.IsNullOrEmpty(DisplayName)
            ? Name
            : DisplayName;
    }
}