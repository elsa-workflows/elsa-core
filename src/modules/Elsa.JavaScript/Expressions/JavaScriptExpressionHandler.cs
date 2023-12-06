using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.JavaScript.Contracts;
using Jint;

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
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var javaScriptExpression = expression.Value.ConvertTo<string>() ?? "";
        return await _javaScriptEvaluator.EvaluateAsync(javaScriptExpression, returnType, context, engine => ConfigureEngine(engine, options), context.CancellationToken);
    }

    private void ConfigureEngine(Engine engine, ExpressionEvaluatorOptions options)
    {
        foreach (var (name, value) in options.Arguments) 
            engine.SetValue(name, value);
    }
}