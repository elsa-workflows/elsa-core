namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a switch statement.
/// </summary>
public class SwitchStatement : Statement
{
    public Expression Expression { get; set; }
    public List<SwitchCase> Cases { get; set; } = new();
    public Block Default { get; set; }
}

/// <summary>
/// A case in a switch.
/// </summary>
public class SwitchCase : AstNode
{
    public Expression Value { get; set; }
    public Block Body { get; set; }
}
