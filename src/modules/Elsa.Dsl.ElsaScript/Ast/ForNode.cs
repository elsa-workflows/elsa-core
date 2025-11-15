namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a for statement.
/// </summary>
public class ForNode : StatementNode
{
    /// <summary>
    /// The initializer statement.
    /// </summary>
    public StatementNode? Initializer { get; set; }

    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionNode? Condition { get; set; }

    /// <summary>
    /// The iterator expression.
    /// </summary>
    public ExpressionNode? Iterator { get; set; }

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}