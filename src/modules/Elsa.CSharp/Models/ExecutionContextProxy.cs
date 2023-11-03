using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.CSharp.Models;

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
    public T? GetVariable<T>(string name) => ExpressionExecutionContext.GetVariableInScope(name).ConvertTo<T>();

    /// <summary>
    /// Sets the value of the specified variable.
    /// </summary>
    public void SetVariable(string name, object? value) => ExpressionExecutionContext.SetVariable(name, value);
}