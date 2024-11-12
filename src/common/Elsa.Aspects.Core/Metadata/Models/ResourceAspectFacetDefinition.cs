using System.Text.Json.Nodes;

namespace Elsa.Aspects.Metadata;

public class ResourceAspectFacetDefinition : ResourceDefinition
{
    public ResourceAspectFacetDefinition(ResourceFacetDefinition resourceFacetDefinition, string name, JsonObject settings)
    {
        Name = name;
        FacetDefinition = resourceFacetDefinition;
        Settings = settings;
    }

    public ResourceFacetDefinition FacetDefinition { get; private set; }
    public ResourceAspectDefinition AspectDefinition { get; set; } = default!;
    public ResourceTypeAspectDefinition ResourceTypeAspectDefinition { get; set; } = default!;
}