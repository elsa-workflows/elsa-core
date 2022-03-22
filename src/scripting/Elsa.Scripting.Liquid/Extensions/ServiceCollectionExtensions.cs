using Elsa.Management.Contracts;
using Elsa.Mediator.Extensions;
using Elsa.Scripting.Liquid.Contracts;
using Elsa.Scripting.Liquid.Expressions;
using Elsa.Scripting.Liquid.Filters;
using Elsa.Scripting.Liquid.Handlers;
using Elsa.Scripting.Liquid.Options;
using Elsa.Scripting.Liquid.Providers;
using Elsa.Scripting.Liquid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Liquid.Extensions;

public static class ServiceCollectionExtensions
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