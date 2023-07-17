using Elsa.Common.Features;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Liquid.Contracts;
using Elsa.Liquid.Expressions;
using Elsa.Liquid.Filters;
using Elsa.Liquid.Handlers;
using Elsa.Liquid.Providers;
using Elsa.Liquid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Liquid.Features;

/// <summary>
/// Configures liquid functionality.
/// </summary>
[DependsOn(typeof(MemoryCacheFeature))]
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class LiquidFeature : FeatureBase
{
    /// <inheritdoc />
    public LiquidFeature(IModule serviceConfiguration) : base(serviceConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Services
            .AddHandlersFrom<ConfigureLiquidEngine>()
            .AddSingleton<IExpressionSyntaxProvider, LiquidExpressionSyntaxProvider>()
            .AddSingleton<ILiquidTemplateManager, LiquidTemplateManager>()
            .AddSingleton<LiquidParser>()
            .AddExpressionHandler<LiquidExpressionHandler, LiquidExpression>()
            .AddLiquidFilter<JsonFilter>("json")
            .AddLiquidFilter<Base64Filter>("base64")
            .AddLiquidFilter<DictionaryKeysFilter>("keys")
            ;
    }
}