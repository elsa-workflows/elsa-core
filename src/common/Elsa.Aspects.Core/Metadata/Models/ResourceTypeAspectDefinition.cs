using System.Text.Json.Nodes;

namespace Elsa.Aspects.Metadata;

public class ResourceTypeAspectDefinition : ResourceDefinition
{
    public ResourceTypeAspectDefinition(string name, ResourceAspectDefinition resourceAspectDefinition, JsonObject settings)
    {
        Name = name;
        AspectDefinition = resourceAspectDefinition;
        Settings = settings;

        foreach (var field in AspectDefinition.Facets)
        {
            field.ResourceTypeAspectDefinition = this;
        }
    }

    public ResourceAspectDefinition AspectDefinition { get; private set; }
    public ResourceTypeDefinition ResourceTypeDefinition { get; set; }
}