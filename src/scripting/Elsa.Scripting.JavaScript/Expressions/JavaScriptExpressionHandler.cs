using Elsa.Models;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services;

namespace Elsa.Scripting.JavaScript.Expressions;

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