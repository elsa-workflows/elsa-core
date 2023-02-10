using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.JavaScript.Contracts;

namespace Elsa.JavaScript.Expressions;

public class JavaScriptExpressionHandler : IExpressionHandler
{
    private readonly IJavaScriptEvaluator _javaScriptEvaluator;

    public JavaScriptExpressionHandler(IJavaScriptEvaluator javaScriptEvaluator)
    {
        _javaScriptEvaluator = javaScriptEvaluator;
    }
    
    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var javaScriptExpression = (JavaScriptExpression)expression;
        return await _javaScriptEvaluator.EvaluateAsync(javaScriptExpression.Value, returnType, context);
    }
}