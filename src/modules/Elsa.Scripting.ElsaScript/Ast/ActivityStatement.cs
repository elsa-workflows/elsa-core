namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents an activity invocation.
/// </summary>
public class ActivityStatement : Statement
{
    public bool IsListen { get; set; }
    public ActivityCall Call { get; set; } = null!;
    public string? Alias { get; set; } // optional 'as id'
}

/// <summary>
/// Represents an activity call with name and arguments.
/// </summary>
public class ActivityCall : AstNode
{
    public string Name { get; set; } = null!;
    public List<Argument> Arguments { get; set; } = new();
}

/// <summary>
/// Argument: either named or positional.
/// </summary>
public class Argument : AstNode
{
    public string? Name { get; set; } // null for positional
    public Expression Value { get; set; } = null!;
}
