namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a block of statements (maps to Sequence).
/// </summary>
public class BlockNode : StatementNode
{
    /// <summary>
    /// The statements in the block.
    /// </summary>
    public List<StatementNode> Statements { get; set; } = [];
}