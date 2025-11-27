namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents the root of an ElsaScript program, which can contain:
/// - Global use statements
/// - One or more workflow declarations
/// </summary>
public class ProgramNode : AstNode
{
    /// <summary>
    /// Global use statements (imports and expression language settings) that apply to all workflows.
    /// </summary>
    public List<UseNode> GlobalUseStatements { get; set; } = [];

    /// <summary>
    /// Workflow declarations in this program.
    /// </summary>
    public List<WorkflowNode> Workflows { get; set; } = [];
}
