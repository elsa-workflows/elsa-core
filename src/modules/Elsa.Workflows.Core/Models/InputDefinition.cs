namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A definition of a workflow's input.
/// </summary>
public class InputDefinition : ArgumentDefinition
{
    /// <summary>
    /// The UI hint to use for this input. 
    /// </summary>
    public string UIHint { get; set; } = default!;

    /// <summary>
    /// The type of the storage driver to use for this input.
    /// </summary>
    public Type? StorageDriverType { get; set; }
}