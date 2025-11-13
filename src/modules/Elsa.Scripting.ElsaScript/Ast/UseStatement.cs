namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a use statement for imports or directives.
/// </summary>
public class UseStatement : Statement
{
    public string Namespace { get; set; } // e.g., "Elsa.Activities.Console"
    public string Directive { get; set; } // e.g., "expressions js", "strict types"
}
