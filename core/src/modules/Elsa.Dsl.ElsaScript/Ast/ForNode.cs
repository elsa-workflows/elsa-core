namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a range-based for statement (e.g., for (var i = 0 to 10 step 1)).
/// </summary>
public class ForNode : StatementNode
{
    /// <summary>
    /// Whether this for loop declares a new variable (var present in header).
    /// </summary>
    public bool DeclaresVariable { get; set; }

    /// <summary>
    /// The loop variable name.
    /// </summary>
    public string VariableName { get; set; } = null!;

    /// <summary>
    /// The start value expression.
    /// </summary>
    public ExpressionNode Start { get; set; } = null!;

    /// <summary>
    /// The end value expression.
    /// </summary>
    public ExpressionNode End { get; set; } = null!;

    /// <summary>
    /// The step value expression.
    /// </summary>
    public ExpressionNode Step { get; set; } = null!;

    /// <summary>
    /// Whether the end value is inclusive (through) or exclusive (to).
    /// </summary>
    public bool IsInclusive { get; set; }

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}