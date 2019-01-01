using Elsa.ActivityProviders;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Serialization.Tokenizers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
        {
            return services
                .AddSingleton<IIdGenerator, DefaultIdGenerator>()
                .AddScoped<IWorkflowSerializer, WorkflowSerializer>()
                .AddScoped<IWorkflowTokenizer, WorkflowTokenizer>()
                .AddScoped<IWorkflowInvoker, WorkflowInvoker>()
                .AddScoped<IActivityProvider, TypedActivityProvider>()
                .AddScoped<IActivityLibrary, ActivityLibrary>()
                .AddSingleton<ITokenFormatter, JsonTokenFormatter>()
                .AddSingleton<ITokenFormatter, YamlTokenFormatter>()
                .AddSingleton<ITokenFormatter, XmlTokenFormatter>()
                .AddSingleton<ITokenFormatterProvider, TokenFormatterProvider>()
                .AddSingleton<ITokenizerInvoker, TokenizerInvoker>()
                .AddSingleton<ITokenizer, DefaultTokenizer>()
                .AddSingleton<ITokenizer, ActivityTokenizer>()
                .AddSingleton<IExpressionEvaluator, PlainTextEvaluator>()
                .AddSingleton<IExpressionEvaluator, JavaScriptEvaluator>()
                .AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>();
        }
    }
}