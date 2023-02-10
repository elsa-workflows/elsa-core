namespace Elsa.JavaScript.TypeDefinitions.Models;

/// <summary>
/// Represents a type definition document model.
/// </summary>
public sealed class TypeDefinitionsDocument
{
    public ICollection<FunctionDefinition> Functions { get; set; } = new List<FunctionDefinition>();
    public ICollection<TypeDefinition> Types { get; set; } = new List<TypeDefinition>();
}