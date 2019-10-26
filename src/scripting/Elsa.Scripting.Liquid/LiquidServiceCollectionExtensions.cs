using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Liquid
{
    public static class LiquidServiceCollectionExtensions
    {
        public static IServiceCollection AddLiquidExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionEvaluator, LiquidExpressionEvaluator>(ServiceLifetime.Singleton)
                .AddMemoryCache()
                .AddSingleton<ILiquidTemplateManager, LiquidTemplateManager>();
        }
    }
}