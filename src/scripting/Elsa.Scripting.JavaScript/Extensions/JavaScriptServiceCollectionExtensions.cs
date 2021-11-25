using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.Services;
using System;
using Elsa;
using Elsa.Activities.JavaScript;
using Elsa.Expressions;
using Elsa.Options;
using Elsa.Scripting.JavaScript.Typings;
using Elsa.Scripting.JavaScript.Providers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class JavaScriptServiceCollectionExtensions
    {
        public static IServiceCollection AddJavaScriptExpressionEvaluator(this IServiceCollection services)
        {
            return services
                .AddScoped<ITypeScriptDefinitionService, TypeScriptDefinitionService>()
                .AddTypeScriptDefinitionProvider<DotNetTypeScriptDefinitionProvider>()
                .AddScoped<IJavaScriptService, JintJavaScriptEvaluator>()
                .AddTransient(s => new JintEvaluationResultConverterFactory(s).GetConverter())
                .AddTransient<IConvertsEnumerableToObject>(s => new EnumerableResultConverter(default))
                .TryAddProvider<IExpressionHandler, JavaScriptExpressionHandler>(ServiceLifetime.Scoped)
                .AddJavaScriptTypeDefinitionProvider<CommonTypeDefinitionProvider>()
                .AddJavaScriptTypeDefinitionProvider<EnumTypeDefinitionProvider>()
                .AddJavaScriptTypeDefinitionProvider<EnumerableTypeDefinitionProvider>()
                .AddJavaScriptTypeDefinitionProvider<WorkflowContextTypeDefinitionProvider>()
                .AddJavaScriptTypeDefinitionProvider<WorkflowVariablesTypeDefinitionProvider>()
                .AddJavaScriptTypeDefinitionProvider<BlacklistedTypeDefinitionProvider>()
                .AddSingleton<IActivityTypeDefinitionRenderer, DefaultActivityTypeDefinitionRenderer>()
                .AddNotificationHandlers(typeof(JavaScriptServiceCollectionExtensions));
        }

        public static IServiceCollection AddJavaScriptTypeDefinitionProvider<T>(this IServiceCollection services) where T: class, ITypeDefinitionProvider => services.AddScoped<ITypeDefinitionProvider, T>();

        public static IServiceCollection AddTypeScriptDefinitionProvider<T>(this IServiceCollection services) where T : class, ITypeScriptDefinitionProvider => services.AddScoped<ITypeScriptDefinitionProvider, T>();

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