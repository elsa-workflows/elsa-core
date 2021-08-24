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
                .AddLiquidFilter<SignalTokenFilter>("signal_token")
                .AddLiquidFilter<ActivityOutputFilter>("output");
        }
        
        public static IServiceCollection AddLiquidFilter<T>(this IServiceCollection services, string name) where T : class, ILiquidFilter
        {
            services.Configure<LiquidOptions>(options => options.FilterRegistrations[name] = typeof(T));
            services.AddScoped<T>();
            return services;
        }
        
        public static IServiceCollection RegisterLiquidTag(this IServiceCollection services, Action<LiquidParser> configure)
        {
            services.Configure<LiquidOptions>(options => options.ParserConfiguration.Add(configure));
            return services;
        }

        /// <summary>
        /// Enables access to .NET configuration via the Configuration keyword. Do not
        /// enable this option if you execute user supplied (or otherwise untrusted)
        /// workflows.
        /// </summary>
        public static IServiceCollection EnableLiquidConfigurationAccess(this IServiceCollection services)
        {
            services.Configure<LiquidOptions>(options => options.EnableConfigurationAccess = true);
            return services;
        }
    }
}