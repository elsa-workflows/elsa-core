using Elsa.Expressions.Configuration;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Services;
using Elsa.Liquid.Expressions;
using Elsa.Liquid.Extensions;
using Elsa.Liquid.Filters;
using Elsa.Liquid.Handlers;
using Elsa.Liquid.Implementations;
using Elsa.Liquid.Providers;
using Elsa.Liquid.Services;
using Elsa.Mediator.Configuration;
using Elsa.Mediator.Extensions;
using Elsa.MemoryCache.Configuration;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Attributes;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Liquid.Configuration;

[Dependency(typeof(MemoryCacheConfigurator))]
[Dependency(typeof(MediatorConfigurator))]
[Dependency(typeof(ExpressionsConfigurator))]
public class LiquidConfigurator : ConfiguratorBase
{
    public LiquidConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
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