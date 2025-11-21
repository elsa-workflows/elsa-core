namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Base class for all AST nodes in ElsaScript.
/// </summary>
public abstract class AstNode
{
    /// <summary>
    /// The line number in the source where this node starts.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// The column number in the source where this node starts.
    /// </summary>
    public int Column { get; set; }
}
