using Elsa.Extensions;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.Services;
using Microsoft.Extensions.Options;
using System;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class JavaScriptServiceCollectionExtensions
    {
        public static IServiceCollection AddJavaScriptExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .TryAddProvider<IExpressionHandler, JavaScriptHandler>(ServiceLifetime.Scoped)
                .AddNotificationHandlers(typeof(JavaScriptServiceCollectionExtensions))
                .AddTypeAlias(typeof(JavaScriptExpression<>), "JavaScriptExpression");
        }

        public static IServiceCollection WithJavaScriptOptions(this IServiceCollection services, Action<OptionsBuilder<ScriptOptions>> options)
        {
            var scriptOptions = services.AddOptions<ScriptOptions>();
            options(scriptOptions);

            return services;
        }
    }
}