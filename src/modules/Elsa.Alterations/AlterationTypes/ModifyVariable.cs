using Elsa.Alterations.Core.Abstractions;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Modifies a variable.
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