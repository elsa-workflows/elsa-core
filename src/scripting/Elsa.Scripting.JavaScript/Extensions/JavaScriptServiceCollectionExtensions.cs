using Elsa.Extensions;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript.Extensions
{
    public static class JavaScriptServiceCollectionExtensions
    {
        public static IServiceCollection AddJavaScriptExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionEvaluator, JavaScriptExpressionEvaluator>(ServiceLifetime.Scoped)
                .AddNotificationHandlers(typeof(JavaScriptServiceCollectionExtensions));
        }
    }
}