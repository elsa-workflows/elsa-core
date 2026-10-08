using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Expressions.Python.Contracts;

namespace Elsa.Expressions.Python.Expressions;

/// <summary>
/// Evaluates C# expressions.
/// </summary>
public class PythonExpressionHandler : IExpressionHandler
{
    private readonly IPythonEvaluator _evaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonExpressionHandler"/> class.
    /// </summary>
    public PythonExpressionHandler(IPythonEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var pythonExpression = expression.Value.ConvertTo<string>() ?? "";
        return await _evaluator.EvaluateAsync(pythonExpression, returnType, context);
    }
}