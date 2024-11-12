namespace Elsa.Aspects.Metadata;

public class ResourceFacetDefinition(string name)
{
    public string Name { get; private set; } = name;
}