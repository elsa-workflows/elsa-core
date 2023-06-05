using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Liquid.Contracts;

namespace Elsa.Liquid.Expressions;

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
    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var liquidExpression = (LiquidExpression)expression;
        var renderedString = await _liquidTemplateManager.RenderAsync(liquidExpression.Value, context);
        return renderedString.ConvertTo(returnType);
    }
}