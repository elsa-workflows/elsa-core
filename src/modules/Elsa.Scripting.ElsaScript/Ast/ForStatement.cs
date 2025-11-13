namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a for statement.
/// </summary>
public class ForStatement : Statement
{
    public Statement Initializer { get; set; }
    public Expression Condition { get; set; }
    public Statement Iterator { get; set; }
    public Block Body { get; set; }
}
