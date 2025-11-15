namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a while statement.
/// </summary>
public class WhileStatement : Statement
{
    public Expression Condition { get; set; }
    public Block Body { get; set; }
}
