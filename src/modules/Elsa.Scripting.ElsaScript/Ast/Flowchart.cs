namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a flowchart container.
/// </summary>
public class Flowchart : Statement
{
    public List<FlowNode> Nodes { get; set; } = new();
    public List<FlowEdge> Edges { get; set; } = new();
    public string? Entry { get; set; }
}

/// <summary>
/// A node in the flowchart.
/// </summary>
public class FlowNode : AstNode
{
    public string Label { get; set; } = null!;
    public AstNode ActivityOrSequence { get; set; } = null!; // ActivityStatement or Block
}

/// <summary>
/// An edge in the flowchart.
/// </summary>
public class FlowEdge : AstNode
{
    public string FromLabel { get; set; }
    public string Outcome { get; set; } // defaults to "Done"
    public string ToLabel { get; set; }
}
