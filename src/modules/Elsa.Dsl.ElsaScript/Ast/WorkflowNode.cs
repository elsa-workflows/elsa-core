namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a workflow definition in ElsaScript.
/// </summary>
public class WorkflowNode : AstNode
{
    /// <summary>
    /// The workflow identifier used in the DSL (the string after "workflow" keyword).
    /// This also serves as the default DefinitionId if not explicitly specified in metadata.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Workflow metadata properties (DisplayName, Description, DefinitionId, Version, etc.)
    /// These are specified in the optional parenthesized argument list after the workflow id.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// The use statements (imports and expression language settings) at workflow scope.
    /// </summary>
    public List<UseNode> UseStatements { get; set; } = [];

    /// <summary>
    /// The body of the workflow (a sequence of statements).
    /// </summary>
    public List<StatementNode> Body { get; set; } = [];
}