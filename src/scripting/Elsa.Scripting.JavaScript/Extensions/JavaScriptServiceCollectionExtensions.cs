using Elsa.Extensions;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

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

        public static IServiceCollection ConfigureJavaScriptOptions(this IServiceCollection services, Action<OptionsBuilder<ScriptOptions>> options)
        {
            var scriptOptions = services.AddOptions<ScriptOptions>();
            options(scriptOptions);
            
            return services;
        }
    }
}