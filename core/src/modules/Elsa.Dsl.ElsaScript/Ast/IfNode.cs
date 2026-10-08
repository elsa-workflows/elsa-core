namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents an if statement.
/// </summary>
public class IfNode : StatementNode
{
    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionNode Condition { get; set; } = null!;

    /// <summary>
    /// The then branch.
    /// </summary>
    public StatementNode Then { get; set; } = null!;

    /// <summary>
    /// The optional else branch.
    /// </summary>
    public StatementNode? Else { get; set; }
}