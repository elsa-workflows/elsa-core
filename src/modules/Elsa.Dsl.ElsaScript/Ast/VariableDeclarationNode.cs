namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a variable declaration.
/// </summary>
public class VariableDeclarationNode : StatementNode
{
    /// <summary>
    /// The variable kind (var, let, const).
    /// </summary>
    public VariableKind Kind { get; set; }

    /// <summary>
    /// The variable name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The initial value expression.
    /// </summary>
    public ExpressionNode? Value { get; set; }
}