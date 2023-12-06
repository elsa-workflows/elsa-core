namespace Elsa.Expressions.Models;

/// <summary>
/// Contains additional options for the expression evaluator.
/// </summary>
public class ExpressionEvaluatorOptions
{
    /// <summary>
    /// An extra set of variables to add to the expression context.
    /// </summary>
    public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
}