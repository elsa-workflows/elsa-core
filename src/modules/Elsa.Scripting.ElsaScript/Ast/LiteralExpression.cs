namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a literal value (number, string, bool).
/// </summary>
public class LiteralExpression : Expression
{
    public object Value { get; set; }
}
