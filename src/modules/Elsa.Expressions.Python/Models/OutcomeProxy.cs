using Elsa.Expressions.Models;
// ReSharper disable InconsistentNaming

namespace Elsa.Expressions.Python.Models;

/// <summary>
/// Provides access to activity outcomes.
/// </summary>
public class OutcomeProxy
{
    /// <summary>
    /// Gets the key of the outcomes property.
    /// </summary>
    public static readonly object OutcomePropertiesKey = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OutcomeProxy"/> class.
    /// </summary>
    public OutcomeProxy(ExpressionExecutionContext expressionExecutionContext)
    {
        ExpressionExecutionContext = expressionExecutionContext;
    }

    private ExpressionExecutionContext ExpressionExecutionContext { get; }

    /// <summary>
    /// Sets the outcome of the current activity.
    /// </summary>
    /// <param name="outcomeNames">The names of the outcomes.</param>
    public void Set(params string[] outcomeNames)
    {
        ExpressionExecutionContext.TransientProperties[OutcomePropertiesKey] = outcomeNames;
    }
}