namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a foreach statement.
/// </summary>
public class ForEachStatement : Statement
{
    public string VariableName { get; set; }
    public Expression Items { get; set; }
    public Block Body { get; set; }
}
