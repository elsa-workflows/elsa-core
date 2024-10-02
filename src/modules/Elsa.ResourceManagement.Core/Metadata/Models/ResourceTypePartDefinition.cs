using System.Text.Json.Nodes;

namespace Elsa.ResourceManagement.Metadata.Models;

public class ResourceTypePartDefinition : ResourceDefinition
{
    public ResourceTypePartDefinition(string name, ResourcePartDefinition resourcePartDefinition, JsonObject settings)
    {
        Name = name;
        PartDefinition = resourcePartDefinition;
        Settings = settings;

        foreach (var field in PartDefinition.Fields)
        {
            field.ResourceTypePartDefinition = this;
        }
    }

    public ResourcePartDefinition PartDefinition { get; private set; }
    public ResourceTypeDefinition ResourceTypeDefinition { get; set; }
}