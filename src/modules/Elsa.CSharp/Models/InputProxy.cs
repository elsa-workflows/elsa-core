using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.CSharp.Models;

/// <summary>
/// Provides access to workflow inputs.
/// </summary>
public class InputProxy
{
    private readonly ExpressionExecutionContext _expressionExecutionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputProxy"/> class.
    /// </summary>
    public InputProxy(ExpressionExecutionContext expressionExecutionContext)
    {
        _expressionExecutionContext = expressionExecutionContext;
    }

    /// <summary>
    /// Gets the value of the specified input.
    /// </summary>
    public object? Get(string name) => _expressionExecutionContext.GetInput(name);
    
    /// <summary>
    /// Gets the value of the specified input.
    /// </summary>
    public T? Get<T>(string name) => _expressionExecutionContext.GetInput<T>(name);
}