namespace Elsa.Scripting.ElsaScript.Ast;

public class InlineStatement : Statement
{
    public InlineExpression Expression { get; set; } = null!;
}

