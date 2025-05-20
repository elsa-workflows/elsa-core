using Elsa.Expressions.Models;

namespace Elsa.Scripting.JavaScript.Models;

/// <summary>
/// Represents an expression.
/// </summary>
public class JavaScriptExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Expression"/> representing a JavaScript expression.
    /// </summary>
    public static Expression Create(string value) => new("JavaScript", value);
}