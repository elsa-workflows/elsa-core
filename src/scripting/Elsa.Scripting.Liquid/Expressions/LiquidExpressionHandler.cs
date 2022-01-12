using Elsa.Contracts;
using Elsa.Helpers;
using Elsa.Models;
using Elsa.Scripting.Liquid.Contracts;
using Elsa.Scripting.Liquid.Services;

namespace Elsa.Scripting.Liquid.Expressions;

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