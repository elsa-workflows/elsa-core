using Elsa.Scripting.Liquid.Options;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Liquid.Extensions
{
    public static class LiquidServiceCollectionExtensions
    {
        public static IServiceCollection AddLiquidExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionEvaluator, LiquidExpressionEvaluator>(ServiceLifetime.Singleton)
                .AddMemoryCache()
                .AddMediatR(typeof(LiquidServiceCollectionExtensions))
                .AddSingleton<ILiquidTemplateManager, LiquidTemplateManager>();
        }
        
        public static IServiceCollection AddLiquidFilter<T>(this IServiceCollection services, string name) where T : class, ILiquidFilter
        {
            services.Configure<LiquidOptions>(options => options.FilterRegistrations.Add(name, typeof(T)));
            services.AddScoped<T>();
            return services;
        }
    }
}