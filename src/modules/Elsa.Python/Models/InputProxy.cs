using Elsa.Expressions.Models;

namespace Elsa.Python.Models;

/// <summary>
/// A wrapper for accessing activity input values from Python.
/// </summary>
public class InputProxy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputProxy"/> class.
    /// </summary>
    public InputProxy(ExpressionExecutionContext context)
    {
        Context = context;
    }

    /// <summary>
    /// Gets the expression execution context.
    /// </summary>
    public ExpressionExecutionContext Context { get; set; }
}