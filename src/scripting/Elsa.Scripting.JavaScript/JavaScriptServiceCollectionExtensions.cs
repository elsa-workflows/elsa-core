using Elsa.Expressions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript
{
    public static class JavaScriptServiceCollectionExtensions
    {
        public static IServiceCollection AddJavaScriptExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionEvaluator, JavaScriptEvaluator>(ServiceLifetime.Singleton)
                .AddSingleton<IScriptEngineConfigurator, CommonScriptEngineConfigurator>()
                .AddSingleton<IScriptEngineConfigurator, DateTimeScriptEngineConfigurator>();
        }
    }
}