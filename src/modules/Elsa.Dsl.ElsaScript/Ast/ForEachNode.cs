namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a foreach statement.
/// </summary>
public class ForEachNode : StatementNode
{
    /// <summary>
    /// The iterator variable name.
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// The collection expression.
    /// </summary>
    public ExpressionNode Collection { get; set; } = null!;

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}