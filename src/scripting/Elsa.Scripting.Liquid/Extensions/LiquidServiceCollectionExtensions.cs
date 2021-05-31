using System;
using Elsa.Expressions;
using Elsa.Scripting.Liquid.Filters;
using Elsa.Scripting.Liquid.Options;
using Elsa.Scripting.Liquid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Liquid.Extensions
{
    public static class LiquidServiceCollectionExtensions
    {
        public static IServiceCollection AddLiquidExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionHandler, LiquidHandler>(ServiceLifetime.Scoped)
                .AddMemoryCache()
                .AddNotificationHandlers(typeof(LiquidServiceCollectionExtensions))
                .AddScoped<ILiquidTemplateManager, LiquidTemplateManager>()
                .AddSingleton<LiquidParser>()
                .AddLiquidFilter<JsonFilter>("json")
                .AddLiquidFilter<Base64Filter>("base64")
                .AddLiquidFilter<WorkflowDefinitionIdFilter>("workflow_definition_id")
                .AddLiquidFilter<SignalTokenFilter>("signal_token");
        }
        
        public static IServiceCollection AddLiquidFilter<T>(this IServiceCollection services, string name) where T : class, ILiquidFilter
        {
            services.Configure<LiquidOptions>(options => options.FilterRegistrations.Add(name, typeof(T)));
            services.AddScoped<T>();
            return services;
        }
        
        public static IServiceCollection RegisterLiquidTag(this IServiceCollection services, Action<LiquidParser> configure)
        {
            services.Configure<LiquidOptions>(options => options.ParserConfiguration.Add(configure));
            return services;
        }
    }
}