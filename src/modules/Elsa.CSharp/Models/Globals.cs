using Elsa.Expressions.Models;

namespace Elsa.CSharp.Models;

/// <summary>
/// Provides access to global objects, such as the workflow execution context.
/// </summary>
public partial class Globals
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Globals"/> class.
    /// </summary>
    public Globals(ExpressionExecutionContext expressionExecutionContext)
    {
        ExpressionExecutionContext = expressionExecutionContext;
        ExecutionContext = new ExecutionContextProxy(expressionExecutionContext);
    }

    /// <summary>
    /// Gets the current execution context.
    /// </summary>
    public ExecutionContextProxy ExecutionContext { get; }
    private ExpressionExecutionContext ExpressionExecutionContext { get; }
}