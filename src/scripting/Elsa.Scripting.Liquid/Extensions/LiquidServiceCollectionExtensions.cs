using Elsa.Extensions;
using Elsa.Scripting.Liquid.Filters;
using Elsa.Scripting.Liquid.Options;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Liquid.Extensions
{
    public static class LiquidServiceCollectionExtensions
    {
        public static IServiceCollection AddLiquidExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionEvaluator, LiquidExpressionEvaluator>(ServiceLifetime.Scoped)
                .AddMemoryCache()
                .AddNotificationHandlers(typeof(LiquidServiceCollectionExtensions))
                .AddScoped<ILiquidTemplateManager, LiquidTemplateManager>()
                .AddLiquidFilter<JsonFilter>("json");
        }
        
        public static IServiceCollection AddLiquidFilter<T>(this IServiceCollection services, string name) where T : class, ILiquidFilter
        {
            services.Configure<LiquidOptions>(options => options.FilterRegistrations.Add(name, typeof(T)));
            services.AddScoped<T>();
            return services;
        }
    }
}