namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents an identifier (variable name).
/// </summary>
public class IdentifierExpression : Expression
{
    public string Name { get; set; }
}
