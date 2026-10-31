namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a connection in a flowchart.
/// </summary>
public class ConnectionNode : AstNode
{
    /// <summary>
    /// The source label.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The optional outcome.
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// The target label.
    /// </summary>
    public string Target { get; set; } = string.Empty;
}