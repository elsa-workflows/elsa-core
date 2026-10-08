using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Expressions.Liquid.Contracts;

namespace Elsa.Expressions.Liquid.Expressions;

/// <summary>
/// Evaluates a Liquid expression.
/// </summary>
public class LiquidExpressionHandler : IExpressionHandler
{
    private readonly ILiquidTemplateManager _liquidTemplateManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiquidExpressionHandler"/> class.
    /// </summary>
    public LiquidExpressionHandler(ILiquidTemplateManager liquidTemplateManager)
    {
        _liquidTemplateManager = liquidTemplateManager;
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var liquidExpression = expression.Value.ConvertTo<string>() ?? "";
        var renderedString = await _liquidTemplateManager.RenderAsync(liquidExpression, context);
        return renderedString.ConvertTo(returnType);
    }
}