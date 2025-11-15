namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents an inline expression with optional language prefix.
/// </summary>
public class InlineExpression : Expression
{
    public string Language { get; set; } // e.g., "js", "cs", null for default
    public string Code { get; set; }
}
