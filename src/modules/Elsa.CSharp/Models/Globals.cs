using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;

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
    
    /// <summary>
    /// Gets the ID of the current workflow instance.
    /// </summary>
    public string WorkflowInstanceId => ExpressionExecutionContext.GetWorkflowExecutionContext().Id;
    
    /// <summary>
    /// Gets or sets the correlation ID of the current workflow instance.
    /// </summary>
    public string? CorrelationId
    {
        get => ExpressionExecutionContext.GetWorkflowExecutionContext().CorrelationId;
        set => ExpressionExecutionContext.GetWorkflowExecutionContext().CorrelationId = value;
    }
    
    /// <summary>
    /// Gets the value of the specified variable.
    /// </summary>
    public T? GetVariable<T>(string name) => ExpressionExecutionContext.GetVariableInScope(name).ConvertTo<T>();

    /// <summary>
    /// Sets the value of the specified variable.
    /// </summary>
    public void SetVariable(string name, object? value) => ExpressionExecutionContext.SetVariable(name, value);
    
    /// <summary>
    /// Gets the value of the specified input.
    /// </summary>
    /// <param name="name">The name of the input.</param>
    /// <returns>The value of the input.</returns>
    public object? GetInput(string name) => ExpressionExecutionContext.GetInput(name);
    
    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="activityIdOrName">The ID or name of the activity that produced the output.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <returns>The value of the output.</returns>
    public object? GetOutputFrom(string activityIdOrName, string? outputName = default) => ExpressionExecutionContext.GetOutput(activityIdOrName, outputName);
    
    /// <summary>
    /// Gets the result of the last activity that executed.
    /// </summary>
    public object? GetLastResult() => ExpressionExecutionContext.GetLastResult();
    
    private ExpressionExecutionContext ExpressionExecutionContext { get; }
}