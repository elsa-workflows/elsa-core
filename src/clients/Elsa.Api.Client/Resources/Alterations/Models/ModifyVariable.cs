namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// Modifies a variable in a workflow instance alteration
/// </summary>
public class ModifyVariable : AlterationBase
{
    /// <summary>
    /// The ID of the variable to modify.
    /// </summary>
    public string VariableId { get; set; } = default!;

    /// <summary>
    ///  The new value of the variable.
    /// </summary>
    public object? Value { get; set; }

}