using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.Services;
using System;
using Elsa;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript.Typings;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class JavaScriptServiceCollectionExtensions
    {
        public static IServiceCollection AddJavaScriptExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .AddSingleton<ITypeScriptDefinitionService, TypeScriptDefinitionService>()
                .AddSingleton<ITypeDefinitionProvider, PrimitiveTypeDefinitionProvider>()
                .AddSingleton<ITypeDefinitionProvider, EnumTypeDefinitionProvider>()
                .TryAddProvider<IExpressionHandler, JavaScriptExpressionHandler>(ServiceLifetime.Scoped)
                .AddNotificationHandlers(typeof(JavaScriptServiceCollectionExtensions));
        }

        public static IServiceCollection WithJavaScriptOptions(this IServiceCollection services, Action<ScriptOptions> configureOptions)
        {
            services.Configure(configureOptions);

            return services;
        }
    }
}