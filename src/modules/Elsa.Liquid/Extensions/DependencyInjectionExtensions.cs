using Elsa.Expressions.Extensions;
using Elsa.Expressions.Services;
using Elsa.Liquid.Expressions;
using Elsa.Liquid.Filters;
using Elsa.Liquid.Handlers;
using Elsa.Liquid.Implementations;
using Elsa.Liquid.Options;
using Elsa.Liquid.Providers;
using Elsa.Liquid.Services;
using Elsa.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Liquid.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddLiquidExpressions(this IServiceCollection services)
    {
        return services
            .AddMemoryCache()
            .AddHandlersFrom<ConfigureLiquidEngine>()
            .AddSingleton<IExpressionSyntaxProvider, LiquidExpressionSyntaxProvider>()
            .AddSingleton<ILiquidTemplateManager, LiquidTemplateManager>()
            .AddSingleton<LiquidParser>()
            .AddExpressionHandler<LiquidExpressionHandler, LiquidExpression>()
            .AddLiquidFilter<JsonFilter>("json")
            .AddLiquidFilter<Base64Filter>("base64");
    }
    
    public static IServiceCollection AddLiquidFilter<T>(this IServiceCollection services, string name) where T : class, ILiquidFilter
    {
        services.Configure<FluidOptions>(options => options.FilterRegistrations[name] = typeof(T));
        services.AddScoped<T>();
        return services;
    }
        
    public static IServiceCollection RegisterLiquidTag(this IServiceCollection services, Action<LiquidParser> configure)
    {
        services.Configure<FluidOptions>(options => options.ParserConfiguration.Add(configure));
        return services;
    }

    /// <summary>
    /// Enables access to .NET configuration via the Configuration keyword.
    /// Do not enable this option if you execute user supplied (or otherwise untrusted) workflows.
    /// </summary>
    public static IServiceCollection EnableLiquidConfigurationAccess(this IServiceCollection services)
    {
        services.Configure<FluidOptions>(options => options.AllowConfigurationAccess = true);
        return services;
    }
}