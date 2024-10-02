using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Models;

public class ResourcePartDefinition : ResourceDefinition
{
    public ResourcePartDefinition(string? name)
    {
        Name = name ?? string.Empty;
        Fields = [];
        Settings = [];
    }

    public ResourcePartDefinition(string name, IEnumerable<ResourcePartFieldDefinition> fields, JsonObject settings)
    {
        Name = name;
        Fields = fields.ToList();
        Settings = settings.Clone();

        foreach (var field in Fields)
            field.PartDefinition = this;
    }

    public IEnumerable<ResourcePartFieldDefinition> Fields { get; private set; }
}