namespace Elsa.Scripting.JavaScript.TypeDefinitions.Models;

/// <summary>
/// Represents a variable definition.
/// </summary>
public class VariableDefinition
{
    public VariableDefinition()
    {
    }
    
    public VariableDefinition(string name, string type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
}