using Elsa.Expressions.CSharp.Contracts;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

namespace Elsa.Expressions.CSharp.Expressions;

/// <summary>
/// Evaluates C# expressions.
/// </summary>
public class CSharpExpressionHandler : IExpressionHandler
{
    private readonly ICSharpEvaluator _cSharpEvaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpExpressionHandler"/> class.
    /// </summary>
    public CSharpExpressionHandler(ICSharpEvaluator cSharpEvaluator)
    {
        _cSharpEvaluator = cSharpEvaluator;
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var script = expression.Value.ConvertTo<string>() ?? "";
        return await _cSharpEvaluator.EvaluateAsync(script, returnType, context, options);
    }
}