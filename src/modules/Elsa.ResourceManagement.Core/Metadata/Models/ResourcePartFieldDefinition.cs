using System.Text.Json.Nodes;

namespace Elsa.ResourceManagement.Metadata.Models;

public class ResourcePartFieldDefinition : ResourceDefinition
{
    public ResourcePartFieldDefinition(ResourceFieldDefinition resourceFieldDefinition, string name, JsonObject settings)
    {
        Name = name;
        FieldDefinition = resourceFieldDefinition;
        Settings = settings;
    }

    public ResourceFieldDefinition FieldDefinition { get; private set; }
    public ResourcePartDefinition PartDefinition { get; set; }
    public ResourceTypePartDefinition ResourceTypePartDefinition { get; set; }
}