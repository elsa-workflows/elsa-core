using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.Services;
using System;
using Elsa;
using Elsa.Activities.JavaScript;
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
                .AddScoped<ITypeScriptDefinitionService, TypeScriptDefinitionService>()
                .AddScoped<ITypeDefinitionProvider, PrimitiveTypeDefinitionProvider>()
                .AddScoped<ITypeDefinitionProvider, EnumTypeDefinitionProvider>()
                .AddScoped<ITypeDefinitionProvider, EnumerableTypeDefinitionProvider>()
                .AddScoped<IJavaScriptService, JintJavaScriptEvaluator>()
                .AddTransient(s => new JintEvaluationResultConverterFactory(s).GetConverter())
                .AddTransient<IConvertsEnumerableToObject>(s => new EnumerableResultConverter(default))
                .TryAddProvider<IExpressionHandler, JavaScriptExpressionHandler>(ServiceLifetime.Scoped)
                .AddNotificationHandlers(typeof(JavaScriptServiceCollectionExtensions));
        }

        public static IServiceCollection WithJavaScriptOptions(this IServiceCollection services, Action<ScriptOptions> configureOptions)
        {
            services.Configure(configureOptions);

            return services;
        }

        public static ElsaOptionsBuilder AddJavaScriptActivities(this ElsaOptionsBuilder options)
        {
            return options.AddActivitiesFrom<RunJavaScript>();
        }
    }
}