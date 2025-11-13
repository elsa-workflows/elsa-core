namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a template string with interpolations.
/// </summary>
public class TemplateStringExpression : Expression
{
    public List<TemplatePart> Parts { get; set; } = new();
}

/// <summary>
/// Part of a template string: either text or an expression.
/// </summary>
public class TemplatePart
{
    public string? Text { get; set; }
    public Expression? Expression { get; set; }
}
