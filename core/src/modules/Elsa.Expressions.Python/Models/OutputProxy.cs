using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
// ReSharper disable InconsistentNaming

namespace Elsa.Expressions.Python.Models;

/// <summary>
/// Provides access to activity outputs.
/// </summary>
public class OutputProxy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputProxy"/> class.
    /// </summary>
    public OutputProxy(ExpressionExecutionContext expressionExecutionContext)
    {
        Context = expressionExecutionContext;
    }
    
    /// <summary>
    /// Gets the expression execution context.
    /// </summary>
    public ExpressionExecutionContext Context { get; }

    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    public object? Get(string activityIdOrName, string? outputName = default) => Context.GetOutput(activityIdOrName, outputName);

    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="returnType">The type to convert the output value to.</param>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    public object? Get(Type returnType, string activityIdOrName, string? outputName = default) => Get(activityIdOrName, outputName).ConvertTo(returnType);
    
    /// <summary>
    /// Gets the result of the last activity that executed.
    /// </summary>
    public object? LastResult => Context.GetLastResult();
}