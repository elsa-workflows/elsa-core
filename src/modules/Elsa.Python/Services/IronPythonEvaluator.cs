using Elsa.Expressions.Models;
using Elsa.Python.Contracts;

namespace Elsa.Python.Services;

/// <summary>
/// Evaluates Python expressions using IronPython.
/// </summary>
public class IronPythonEvaluator : IPythonEvaluator
{
    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(string expression, Type returnType, ExpressionExecutionContext context, CancellationToken cancellationToken = default)
    {
        var eng = IronPython.Hosting.Python.CreateEngine();
        var scope = eng.CreateScope();
        var result = eng.Execute(expression, scope);
        return result;
    }
}