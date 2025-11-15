namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a workflow definition in ElsaScript.
/// </summary>
public class WorkflowNode : AstNode
{
    /// <summary>
    /// The name of the workflow.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The use statements (imports and expression language settings).
    /// </summary>
    public List<UseNode> UseStatements { get; set; } = new();

    /// <summary>
    /// The body of the workflow (a sequence of statements).
    /// </summary>
    public List<StatementNode> Body { get; set; } = new();
}
