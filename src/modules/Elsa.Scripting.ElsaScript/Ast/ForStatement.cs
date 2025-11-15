namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a range-based for statement: for varName = start to/through end [step step] { ... }
/// </summary>
public class ForStatement : Statement
{
    /// <summary>
    /// The loop variable name (e.g., "i" in "for i = 0 to 10")
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// The start value expression
    /// </summary>
    public Expression Start { get; set; } = default!;

    /// <summary>
    /// The end value expression
    /// </summary>
    public Expression End { get; set; } = default!;

    /// <summary>
    /// The step expression (defaults to 1)
    /// </summary>
    public Expression? Step { get; set; }

    /// <summary>
    /// True if "through" (inclusive), false if "to" (exclusive)
    /// </summary>
    public bool IsInclusive { get; set; }

    /// <summary>
    /// The loop body
    /// </summary>
    public Block Body { get; set; } = default!;
}
