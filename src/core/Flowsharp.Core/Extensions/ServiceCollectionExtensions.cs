using Flowsharp.Expressions;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Serialization.Tokenizers;
using Microsoft.Extensions.DependencyInjection;

namespace Flowsharp.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFlowsharpCore(this IServiceCollection services)
        {
            return services
                .AddScoped<IWorkflowSerializer, WorkflowSerializer>()
                .AddScoped<IWorkflowTokenizer, WorkflowTokenizer>()
                .AddScoped<IWorkflowInvoker, WorkflowInvoker>()
                .AddScoped<IActivityInvoker, ActivityInvoker>()
                .AddSingleton<ITokenFormatter, YamlTokenFormatter>()
                .AddSingleton<ITokenizerInvoker, TokenizerInvoker>()
                .AddSingleton<ITokenizer, DefaultTokenizer>()
                .AddSingleton<ITokenizer, ActivityTokenizer>()
                .AddSingleton<IExpressionEvaluator, PlainTextEvaluator>()
                .AddSingleton<IExpressionEvaluator, JavaScriptEvaluator>()
                .AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>();
        }
    }
}