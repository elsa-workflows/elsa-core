using Elsa.Scripting.JavaScript.Services;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript.Extensions
{
    public static class JavaScriptServiceCollectionExtensions
    {
        public static IServiceCollection AddJavaScriptExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionEvaluator, JavaScriptExpressionEvaluator>(ServiceLifetime.Scoped)
                .AddMediatR(typeof(JavaScriptServiceCollectionExtensions));
        }
    }
}