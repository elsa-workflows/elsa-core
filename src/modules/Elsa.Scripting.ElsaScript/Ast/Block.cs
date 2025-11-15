namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a block of statements.
/// </summary>
public class Block : AstNode
{
    public List<Statement> Statements { get; set; } = new();
}
