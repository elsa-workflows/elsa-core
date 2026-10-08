namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a foreach statement (e.g., foreach (var item in collection)).
/// </summary>
public class ForEachNode : StatementNode
{
    /// <summary>
    /// Whether this foreach loop declares a new variable (var present in header).
    /// </summary>
    public bool DeclaresVariable { get; set; }

    /// <summary>
    /// The loop variable name.
    /// </summary>
    public string VariableName { get; set; } = null!;

    /// <summary>
    /// The collection expression to iterate over.
    /// </summary>
    public ExpressionNode Collection { get; set; } = null!;

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}
