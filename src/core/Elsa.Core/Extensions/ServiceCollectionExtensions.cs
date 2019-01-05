using Elsa.Expressions;
using Elsa.Harvesters;
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
                .AddScoped<IActivityHarvester, TypedActivityHarvester>()
                .AddScoped<IActivityLibrary, ActivityLibrary>()
                .AddActivityDescriptors<ActivityDescriptors>()
                .AddSingleton<ITokenFormatter, JsonTokenFormatter>()
                .AddSingleton<ITokenFormatter, YamlTokenFormatter>()
                .AddSingleton<ITokenFormatter, XmlTokenFormatter>()
                .AddSingleton<ITokenFormatterProvider, TokenFormatterProvider>()
                .AddSingleton<ITokenizerInvoker, TokenizerInvoker>()
                .AddSingleton<ITokenizer, DefaultTokenizer>()
                .AddSingleton<ITokenizer, ActivityTokenizer>();
        }

        public static IServiceCollection AddWorkflowsInvoker(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorkflowInvoker, WorkflowInvoker>()
                .AddSingleton<IActivityInvoker, ActivityInvoker>()
                .AddSingleton<IActivityDriverRegistry, ActivityDriverRegistry>()
                .AddSingleton<IExpressionEvaluator, PlainTextEvaluator>()
                .AddSingleton<IExpressionEvaluator, JavaScriptEvaluator>()
                .AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>();
        }
    }
}