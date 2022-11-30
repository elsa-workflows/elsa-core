using Elsa.Common.Features;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Features;
using Elsa.Expressions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Liquid.Expressions;
using Elsa.Liquid.Extensions;
using Elsa.Liquid.Filters;
using Elsa.Liquid.Handlers;
using Elsa.Liquid.Implementations;
using Elsa.Liquid.Providers;
using Elsa.Liquid.Services;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Liquid.Features;

[DependsOn(typeof(MemoryCacheFeature))]
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class LiquidFeature : FeatureBase
{
    public LiquidFeature(IModule serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public override void Configure()
    {
        Services
            .AddHandlersFrom<ConfigureLiquidEngine>()
            .AddSingleton<IExpressionSyntaxProvider, LiquidExpressionSyntaxProvider>()
            .AddSingleton<ILiquidTemplateManager, LiquidTemplateManager>()
            .AddSingleton<LiquidParser>()
            .AddExpressionHandler<LiquidExpressionHandler, LiquidExpression>()
            .AddLiquidFilter<JsonFilter>("json")
            .AddLiquidFilter<Base64Filter>("base64");
    }
}