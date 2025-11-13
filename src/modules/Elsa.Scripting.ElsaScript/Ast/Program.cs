namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents the entire program.
/// </summary>
public class Program : AstNode
{
    public List<Statement> Statements { get; set; } = new();
}
