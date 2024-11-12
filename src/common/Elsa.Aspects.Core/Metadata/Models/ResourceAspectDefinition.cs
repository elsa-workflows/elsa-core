using System.Text.Json.Nodes;

namespace Elsa.Aspects.Metadata;

public class ResourceAspectDefinition : ResourceDefinition
{
    public ResourceAspectDefinition(string name)
    {
        Name = name;
        Facets = [];
        Settings = [];
    }

    public ResourceAspectDefinition(string name, IEnumerable<ResourceAspectFacetDefinition> facets, JsonObject settings)
    {
        Name = name;
        Facets = facets.ToList();
        Settings = settings.Clone();

        foreach (var field in Facets)
        {
            field.AspectDefinition = this;
        }
    }

    public IEnumerable<ResourceAspectFacetDefinition> Facets { get; private set; }
}