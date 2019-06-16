using Elsa.Core.Expressions;
using Elsa.Core.Serialization;
using Elsa.Core.Serialization.Formatters;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;

namespace Elsa.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddLocalization();
            services.TryAddSingleton<IIdGenerator, DefaultIdGenerator>();
            services.TryAddSingleton<IClock>(SystemClock.Instance);
            services.TryAddSingleton<ITokenFormatterProvider, TokenFormatterProvider>();
            services.AddSingleton<ITokenFormatter, JsonTokenFormatter>();
            services.AddSingleton<ITokenFormatter, YamlTokenFormatter>();
            services.AddSingleton<ITokenFormatter, XmlTokenFormatter>();
            services.AddSingleton<IExpressionEvaluator, PlainTextEvaluator>();
            services.AddSingleton<IExpressionEvaluator, JavaScriptEvaluator>();
            
            return services;
        }

        public static IServiceCollection AddWorkflowsInvoker(this IServiceCollection services)
        {
            services.AddWorkflowsCore();
            services.TryAddSingleton<IWorkflowInvoker, WorkflowInvoker>();
            services.TryAddSingleton<IActivityInvoker, ActivityInvoker>();
            services.TryAddSingleton<IActivityDriverRegistry, ActivityDriverRegistry>();
            services.TryAddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>();

            return services;
        }
    }
}