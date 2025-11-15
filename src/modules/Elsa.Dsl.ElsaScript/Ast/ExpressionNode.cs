namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Base class for expression nodes.
/// </summary>
public abstract class ExpressionNode : AstNode
{
}

/// <summary>
/// Represents a literal expression.
/// </summary>
public class LiteralNode : ExpressionNode
{
    /// <summary>
    /// The literal value.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Represents an Elsa expression with a specific language.
/// </summary>
public class ElsaExpressionNode : ExpressionNode
{
    /// <summary>
    /// The expression language (e.g., "js", "cs", "py", "liquid").
    /// If null, uses the default language from use statements.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The expression code.
    /// </summary>
    public string Expression { get; set; } = string.Empty;
}

/// <summary>
/// Represents a template string expression.
/// </summary>
public class TemplateStringNode : ExpressionNode
{
    /// <summary>
    /// The parts of the template string (alternating between literals and expressions).
    /// </summary>
    public List<TemplatePartNode> Parts { get; set; } = new();
}

/// <summary>
/// Represents a part of a template string.
/// </summary>
public class TemplatePartNode : AstNode
{
    /// <summary>
    /// Whether this is a literal or expression part.
    /// </summary>
    public bool IsExpression { get; set; }

    /// <summary>
    /// The content (literal text or expression code).
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Represents an identifier (variable reference).
/// </summary>
public class IdentifierNode : ExpressionNode
{
    /// <summary>
    /// The identifier name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents an array literal.
/// </summary>
public class ArrayLiteralNode : ExpressionNode
{
    /// <summary>
    /// The elements of the array.
    /// </summary>
    public List<ExpressionNode> Elements { get; set; } = new();
}

/// <summary>
/// Represents a binary expression (e.g., a + b).
/// </summary>
public class BinaryExpressionNode : ExpressionNode
{
    /// <summary>
    /// The left operand.
    /// </summary>
    public ExpressionNode Left { get; set; } = null!;

    /// <summary>
    /// The operator.
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// The right operand.
    /// </summary>
    public ExpressionNode Right { get; set; } = null!;
}

/// <summary>
/// Represents a unary expression (e.g., ++i, i++).
/// </summary>
public class UnaryExpressionNode : ExpressionNode
{
    /// <summary>
    /// The operator.
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// The operand.
    /// </summary>
    public ExpressionNode Operand { get; set; } = null!;

    /// <summary>
    /// Whether the operator is prefix (true) or postfix (false).
    /// </summary>
    public bool IsPrefix { get; set; }
}
