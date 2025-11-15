using JetBrains.Annotations;

namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a labeled activity in a flowchart.
/// </summary>
[UsedImplicitly]
public class LabeledActivityNode : AstNode
{
    /// <summary>
    /// The label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The activity invocation.
    /// </summary>
    public ActivityInvocationNode Activity { get; set; } = null!;
}