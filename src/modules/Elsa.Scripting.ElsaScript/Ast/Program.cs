namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents the entire program.
/// </summary>
public class Program : AstNode
{
    public WorkflowStatement? Workflow { get; set; }
    public List<Statement> Statements { get; set; } = new();
}
