using Elsa.Expressions;
using Elsa.Harvesters;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Serialization.Tokenizers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IIdGenerator, DefaultIdGenerator>();
            services.TryAddScoped<IWorkflowSerializer, WorkflowSerializer>();
            services.TryAddScoped<IWorkflowTokenizer, WorkflowTokenizer>();
            services.TryAddScoped<IActivityHarvester, TypedActivityHarvester>();
            services.TryAddScoped<IActivityLibrary, ActivityLibrary>();
            services.AddActivityDescriptors<ActivityDescriptors>();
            services.TryAddSingleton<ITokenFormatter, JsonTokenFormatter>();
            services.TryAddSingleton<ITokenFormatter, YamlTokenFormatter>();
            services.TryAddSingleton<ITokenFormatter, XmlTokenFormatter>();
            services.TryAddSingleton<ITokenFormatterProvider, TokenFormatterProvider>();
            services.TryAddSingleton<ITokenizerInvoker, TokenizerInvoker>();
            services.TryAddSingleton<ITokenizer, DefaultTokenizer>();
            services.TryAddSingleton<ITokenizer, ActivityTokenizer>();

            return services;
        }

        public static IServiceCollection AddWorkflowsInvoker(this IServiceCollection services)
        {
            services.TryAddSingleton<IWorkflowInvoker, WorkflowInvoker>();
            services.TryAddSingleton<IActivityInvoker, ActivityInvoker>();
            services.TryAddSingleton<IActivityDriverRegistry, ActivityDriverRegistry>();
            services.TryAddSingleton<IExpressionEvaluator, PlainTextEvaluator>();
            services.TryAddSingleton<IExpressionEvaluator, JavaScriptEvaluator>();
            services.TryAddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>();

            return services;
        }
    }
}