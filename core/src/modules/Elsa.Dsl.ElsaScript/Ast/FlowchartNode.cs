namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a flowchart definition.
/// </summary>
public class FlowchartNode : StatementNode
{
    /// <summary>
    /// Variable declarations scoped to the flowchart.
    /// </summary>
    public List<VariableDeclarationNode> Variables { get; set; } = [];

    /// <summary>
    /// The labeled activities (nodes) in the flowchart.
    /// </summary>
    public List<LabeledActivityNode> Activities { get; set; } = [];

    /// <summary>
    /// The connections (edges) between activities.
    /// </summary>
    public List<ConnectionNode> Connections { get; set; } = [];

    /// <summary>
    /// The optional entry point label.
    /// </summary>
    public string? EntryPoint { get; set; }
}
