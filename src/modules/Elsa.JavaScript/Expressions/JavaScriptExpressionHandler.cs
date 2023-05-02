using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.JavaScript.Contracts;

namespace Elsa.JavaScript.Expressions;

/// <summary>
/// Evaluates JavaScript expressions.
/// </summary>
public class JavaScriptExpressionHandler : IExpressionHandler
{
    private readonly IJavaScriptEvaluator _javaScriptEvaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptExpressionHandler"/> class.
    /// </summary>
    public JavaScriptExpressionHandler(IJavaScriptEvaluator javaScriptEvaluator)
    {
        _javaScriptEvaluator = javaScriptEvaluator;
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var javaScriptExpression = (JavaScriptExpression)expression;
        return await _javaScriptEvaluator.EvaluateAsync(javaScriptExpression.Value, returnType, context);
    }
}