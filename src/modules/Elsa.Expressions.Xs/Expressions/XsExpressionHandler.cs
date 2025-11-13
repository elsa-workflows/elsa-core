using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Expressions.Xs.Contracts;

namespace Elsa.Expressions.Xs.Expressions;

/// <summary>
/// Evaluates XS expressions.
/// </summary>
public class XsExpressionHandler : IExpressionHandler
{
    private readonly IXsEvaluator _xsEvaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="XsExpressionHandler"/> class.
    /// </summary>
    public XsExpressionHandler(IXsEvaluator xsEvaluator)
    {
        _xsEvaluator = xsEvaluator;
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var script = expression.Value.ConvertTo<string>() ?? "";
        return await _xsEvaluator.EvaluateAsync(script, returnType, context, options);
    }
}
