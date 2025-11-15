namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents an if statement.
/// </summary>
public class IfStatement : Statement
{
    public Expression Condition { get; set; } = null!;
    public StatementOrBlock Then { get; set; } = null!;
    public StatementOrBlock? Else { get; set; }
}

/// <summary>
/// Either a single statement or a block.
/// </summary>
public class StatementOrBlock : AstNode
{
    public Statement? Statement { get; set; }
    public Block? Block { get; set; }
}
