using Elsa.Helpers;
using Elsa.Liquid.Services;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Liquid.Expressions;

public class LiquidExpressionHandler : IExpressionHandler
{
    private readonly ILiquidTemplateManager _liquidTemplateManager;

    public LiquidExpressionHandler(ILiquidTemplateManager liquidTemplateManager)
    {
        _liquidTemplateManager = liquidTemplateManager;
    }
    
    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var liquidExpression = (LiquidExpression)expression;
        var renderedString = await _liquidTemplateManager.RenderAsync(liquidExpression.Value, context);
        return renderedString.ConvertTo(returnType);
    }
}