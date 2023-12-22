using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.CSharp.Models;

/// <summary>
/// Provides access to activity outputs.
/// </summary>
public class OutputProxy
{
    private readonly ExpressionExecutionContext _expressionExecutionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputProxy"/> class.
    /// </summary>
    public OutputProxy(ExpressionExecutionContext expressionExecutionContext)
    {
        _expressionExecutionContext = expressionExecutionContext;
    }

    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    public object? Get(string activityIdOrName, string? outputName = default) => _expressionExecutionContext.GetOutput(activityIdOrName, outputName);
    
    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    public T? Get<T>(string activityIdOrName, string? outputName = default) => Get(activityIdOrName, outputName).ConvertTo<T>();
    
    /// <summary>
    /// Gets the result of the last activity that executed.
    /// </summary>
    public object? LastResult => _expressionExecutionContext.GetLastResult();
}