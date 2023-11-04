using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Python.Models;

/// <summary>
/// Provides access to the current execution context.
/// </summary>
[UsedImplicitly]
public partial class ExecutionContextProxy
{
    /// <summary>
    /// Gets the current execution context.
    /// </summary>
    public ExpressionExecutionContext ExpressionExecutionContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContextProxy"/> class.
    /// </summary>
    public ExecutionContextProxy(ExpressionExecutionContext expressionExecutionContext)
    {
        ExpressionExecutionContext = expressionExecutionContext;
    }

    /// <summary>
    /// Gets the value of the specified variable.
    /// </summary>
    public object? get_variable(Type type, string name) => ExpressionExecutionContext.GetVariableInScope(name).ConvertTo(type);

    /// <summary>
    /// Sets the value of the specified variable.
    /// </summary>
    public void set_variable(string name, object? value) => ExpressionExecutionContext.SetVariable(name, value);

    /// <summary>
    /// Gets the workflow instance ID.
    /// </summary>
    public string workflow_instance_id => ExpressionExecutionContext.GetWorkflowExecutionContext().Id;

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? correlation_id
    {
        get => ExpressionExecutionContext.GetWorkflowExecutionContext().CorrelationId;
        set => ExpressionExecutionContext.GetWorkflowExecutionContext().CorrelationId = value;
    }

}