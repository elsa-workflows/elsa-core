namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a workflow declaration with metadata.
/// </summary>
public class WorkflowStatement : Statement
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Block Body { get; set; } = null!;
}

