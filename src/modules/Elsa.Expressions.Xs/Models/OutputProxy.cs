using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.Expressions.Xs.Models;

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
    /// Gets the value of the specified output from the specified activity.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    public object? From(string activityIdOrName, string? outputName = null) => _expressionExecutionContext.GetOutput(activityIdOrName, outputName);
    
    /// <summary>
    /// Gets the value of the specified output from the specified activity.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    public T? From<T>(string activityIdOrName, string? outputName = null) => From(activityIdOrName, outputName).ConvertTo<T>();

    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    [Obsolete("Use From instead.")]
    public object? Get(string activityIdOrName, string? outputName = null) => From(activityIdOrName, outputName);
    
    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    [Obsolete("Use From instead.")]
    public T? Get<T>(string activityIdOrName, string? outputName = null) => From<T>(activityIdOrName, outputName);
    
    /// <summary>
    /// Gets the result of the last activity that executed.
    /// </summary>
    public object? LastResult => _expressionExecutionContext.GetLastResult();
}
