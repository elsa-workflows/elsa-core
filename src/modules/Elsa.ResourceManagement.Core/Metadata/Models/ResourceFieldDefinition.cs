namespace Elsa.ResourceManagement.Metadata.Models;

public class ResourceFieldDefinition(string name)
{
    public string Name { get; private set; } = name;
}