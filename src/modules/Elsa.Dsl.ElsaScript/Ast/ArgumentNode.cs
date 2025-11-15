namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents an argument passed to an activity.
/// </summary>
public class ArgumentNode : AstNode
{
    /// <summary>
    /// The parameter name (null for positional arguments).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The argument value expression.
    /// </summary>
    public ExpressionNode Value { get; set; } = null!;
}
