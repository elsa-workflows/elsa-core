namespace Elsa.JavaScript.TypeDefinitions.Models;

/// <summary>
/// A method definition.
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public FunctionDefinition()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public FunctionDefinition(string name, IEnumerable<ParameterDefinition> parameters)
    {
        Name = name;
        Parameters = parameters.ToList();
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public FunctionDefinition(string name, string? returnType, IEnumerable<ParameterDefinition> parameters)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters.ToList();
    }
    
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