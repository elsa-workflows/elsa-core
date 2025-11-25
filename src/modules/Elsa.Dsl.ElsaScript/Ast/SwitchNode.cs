namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a switch statement.
/// </summary>
public class SwitchNode : StatementNode
{
    /// <summary>
    /// The switch expression.
    /// </summary>
    public ExpressionNode Expression { get; set; } = null!;

    /// <summary>
    /// The cases.
    /// </summary>
    public List<SwitchCaseNode> Cases { get; set; } = [];

    /// <summary>
    /// The optional default case.
    /// </summary>
    public StatementNode? Default { get; set; }
}