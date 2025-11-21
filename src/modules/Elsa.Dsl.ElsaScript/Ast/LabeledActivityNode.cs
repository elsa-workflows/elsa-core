using JetBrains.Annotations;

namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a labeled activity in a flowchart (node declaration).
/// </summary>
[UsedImplicitly]
public class LabeledActivityNode : AstNode
{
    /// <summary>
    /// The label (node name).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The activity statement (can be ActivityInvocationNode, BlockNode, etc.).
    /// </summary>
    public StatementNode Activity { get; set; } = null!;
}