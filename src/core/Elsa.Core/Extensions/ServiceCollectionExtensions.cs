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
            services.AddScoped<IActivityHarvester, TypedActivityHarvester>();
            services.TryAddScoped<IActivityLibrary, ActivityLibrary>();
            services.AddActivityDescriptors<ActivityDescriptors>();
            services.AddSingleton<ITokenFormatter, JsonTokenFormatter>();
            services.AddSingleton<ITokenFormatter, YamlTokenFormatter>();
            services.AddSingleton<ITokenFormatter, XmlTokenFormatter>();
            services.TryAddSingleton<ITokenFormatterProvider, TokenFormatterProvider>();
            services.TryAddSingleton<ITokenizerInvoker, TokenizerInvoker>();
            services.AddSingleton<ITokenizer, DefaultTokenizer>();
            services.AddSingleton<ITokenizer, ActivityTokenizer>();

            return services;
        }

        public static IServiceCollection AddWorkflowsInvoker(this IServiceCollection services)
        {
            services.TryAddSingleton<IWorkflowInvoker, WorkflowInvoker>();
            services.TryAddSingleton<IActivityInvoker, ActivityInvoker>();
            services.TryAddSingleton<IActivityDriverRegistry, ActivityDriverRegistry>();
            services.AddSingleton<IExpressionEvaluator, PlainTextEvaluator>();
            services.AddSingleton<IExpressionEvaluator, JavaScriptEvaluator>();
            services.TryAddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>();

            return services;
        }
    }
}