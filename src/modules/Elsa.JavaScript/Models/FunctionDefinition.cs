namespace Elsa.JavaScript.Models;

/// <summary>
/// A method definition.
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// The name of the method.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The return type name of the method.
    /// </summary>
    public string? ReturnType { get; set; }

    /// <summary>
    /// The parameters of the method.
    /// </summary>
    public ICollection<ParameterDefinition> Parameters { get; set; } = new List<ParameterDefinition>();
}