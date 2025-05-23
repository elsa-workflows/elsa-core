namespace Elsa.Expressions.JavaScript.TypeDefinitions.Models;

/// <summary>
/// Represents a type definition document model.
/// </summary>
public sealed class TypeDefinitionsDocument
{
    /// <summary>
    /// A collection of function definitions.
    /// </summary>
    public ICollection<FunctionDefinition> Functions { get; set; } = new List<FunctionDefinition>();
    
    /// <summary>
    /// A collection of type definitions.
    /// </summary>
    public ICollection<TypeDefinition> Types { get; set; } = new List<TypeDefinition>();
    
    /// <summary>
    /// A collection of global variables.
    /// </summary>
    public ICollection<VariableDefinition> Variables { get; set; } = new List<VariableDefinition>();
}