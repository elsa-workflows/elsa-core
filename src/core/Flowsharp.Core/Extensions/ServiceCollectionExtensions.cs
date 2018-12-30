using Flowsharp.ActivityProviders;
using Flowsharp.Expressions;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Serialization.Tokenizers;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Flowsharp.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
        {
            return services
                .AddSingleton<IIdGenerator, DefaultIdGenerator>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .AddSingleton<IWorkflowTokenizer, WorkflowTokenizer>()
                .AddSingleton<IWorkflowInvoker, WorkflowInvoker>()
                .AddSingleton<IActivityProvider, TypedActivityProvider>()
                .AddSingleton<IActivityLibrary, ActivityLibrary>()
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