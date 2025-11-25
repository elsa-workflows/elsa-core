namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a while statement.
/// </summary>
public class WhileNode : StatementNode
{
    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionNode Condition { get; set; } = null!;

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}