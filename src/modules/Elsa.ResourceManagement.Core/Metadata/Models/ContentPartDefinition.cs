using System.Text.Json.Nodes;
using Elsa.ResourceManagement.Serialization.Extensions;

namespace Elsa.ResourceManagement.Metadata.Models;

public class ContentPartDefinition : ContentDefinition
{
    public ContentPartDefinition(string? name)
    {
        Name = name ?? string.Empty;
        Fields = [];
        Settings = [];
    }

    public ContentPartDefinition(string name, IEnumerable<ContentPartFieldDefinition> fields, JsonObject settings)
    {
        Name = name;
        Fields = fields.ToList();
        Settings = settings.Clone();

        foreach (var field in Fields)
            field.PartDefinition = this;
    }

    public IEnumerable<ContentPartFieldDefinition> Fields { get; private set; }
}