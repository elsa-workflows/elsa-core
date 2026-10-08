namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a use statement (import or expression language configuration).
/// </summary>
public class UseNode : AstNode
{
    /// <summary>
    /// The type of use statement (e.g., "expressions", namespace import).
    /// </summary>
    public UseType Type { get; set; }

    /// <summary>
    /// The value (e.g., "js", "cs", or a namespace).
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// The type of use statement.
/// </summary>
public enum UseType
{
    /// <summary>
    /// Expression language configuration (e.g., "use expressions js;").
    /// </summary>
    Expressions,

    /// <summary>
    /// Namespace import (e.g., "use Elsa.Activities.Console;").
    /// </summary>
    Namespace
}
