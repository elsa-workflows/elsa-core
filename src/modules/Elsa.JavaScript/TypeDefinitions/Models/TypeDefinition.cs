namespace Elsa.JavaScript.TypeDefinitions.Models;


/// <summary>
/// A base type that represents a type.
/// </summary>
public sealed class TypeDefinition
{
    /// <summary>
    /// The keyword that declares this type. E.g. "class", "type" or "interface".
    /// </summary>
    public string DeclarationKeyword { get; set; } = default!;

    /// <summary>
    /// The name of the type.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// A list of fields.
    /// </summary>
    public ICollection<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
    
    /// <summary>
    /// A list of methods.
    /// </summary>
    public ICollection<FunctionDefinition> Methods { get; set; } = new List<FunctionDefinition>();
}