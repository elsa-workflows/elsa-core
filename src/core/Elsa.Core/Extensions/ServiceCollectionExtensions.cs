using System;
using Elsa.Core.Activities.Primitives;
using Elsa.Core.Expressions;
using Elsa.Core.Scripting;
using Elsa.Core.Serialization;
using Elsa.Core.Serialization.Formatters;
using Elsa.Core.Services;
using Elsa.Core.Services.WorkflowBuilders;
using Elsa.Scripting;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;

namespace Elsa.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflows(this IServiceCollection services)
        {
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            return services
                .AddLogging()
                .AddLocalization()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .AddSingleton<ITokenFormatter, JsonTokenFormatter>()
                .AddSingleton<ITokenFormatter, YamlTokenFormatter>()
                .AddSingleton<ITokenFormatter, XmlTokenFormatter>()
                .AddSingleton<IExpressionEvaluator, PlainTextEvaluator>()
                .AddSingleton<IExpressionEvaluator, JavaScriptEvaluator>()
                .AddSingleton<IScriptEngineConfigurator, CommonScriptEngineConfigurator>()
                .AddSingleton<IWorkflowInvoker, WorkflowInvoker>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddSingleton<IActivityInvoker, ActivityInvoker>()
                .AddSingleton<IActivityResolver, ActivityResolver>()
                .AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>()
                .AddSingleton<IWorkflowSerializerProvider, WorkflowSerializerProvider>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddSingleton<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddSingleton<IWorkflowRegistry, WorkflowRegistry>()
                .AddPrimitiveActivities();
        }

        public static IServiceCollection AddActivity<T>(this IServiceCollection services)
            where T : class, IActivity
        {
            return services
                .AddTransient<T>()
                .AddTransient<IActivity>(sp => sp.GetRequiredService<T>());
        }

        private static IServiceCollection AddPrimitiveActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<SetVariable>()
                .AddActivity<ForEach>()
                .AddActivity<Fork>()
                .AddActivity<Join>()
                .AddSingleton<IWorkflowEventHandler>(sp => sp.GetRequiredService<Join>())
                .AddActivity<IfElse>()
                .AddActivity<Switch>();
        }
    }
}