using Elsa.Expressions.Models;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.CSharp.Models;

/// <summary>
/// Provides access to the current execution context.
/// </summary>
[UsedImplicitly]
public class ExecutionContextProxy
{
    private readonly ExpressionExecutionContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContextProxy"/> class.
    /// </summary>
    public ExecutionContextProxy(ExpressionExecutionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the value of the specified variable.
    /// </summary>
    public T GetVariable<T>(string name)
    {
        return (T)_context.GetVariableInScope(name);
    }

    /// <summary>
    /// Sets the value of the specified variable.
    /// </summary>
    public void SetVariable(string name, object? value)
    {
        _context.SetVariable(name, value);
    }
}