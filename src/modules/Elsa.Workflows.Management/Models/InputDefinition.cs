namespace Elsa.Workflows.Management.Models;

/// <summary>
/// A definition of a workflow's input.
/// </summary>
public class InputDefinition : ArgumentDefinition
{
    /// <summary>
    /// The UI hint to use for this input. 
    /// </summary>
    public string UIHint { get; set; } = default!;
}