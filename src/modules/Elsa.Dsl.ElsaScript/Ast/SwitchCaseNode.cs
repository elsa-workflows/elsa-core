namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a switch case.
/// </summary>
public class SwitchCaseNode : AstNode
{
    /// <summary>
    /// The case value.
    /// </summary>
    public ExpressionNode Value { get; set; } = null!;

    /// <summary>
    /// The case body.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}