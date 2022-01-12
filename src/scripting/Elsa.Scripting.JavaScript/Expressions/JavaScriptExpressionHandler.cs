using Elsa.Contracts;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Contracts;

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
            
        // TODO: evaluate expression.
        return await _javaScriptEvaluator.EvaluateAsync(javaScriptExpression.Value, returnType, context);
    }
}