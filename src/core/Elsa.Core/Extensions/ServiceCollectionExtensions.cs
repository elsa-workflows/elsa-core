using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Workflows;
using Elsa.AutoMapper.Extensions;
using Elsa.Expressions;
using Elsa.Mapping;
using Elsa.Runtime;
using Elsa.Scripting;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.StartupTasks;
using Elsa.WorkflowBuilders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;

namespace Elsa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflows(this IServiceCollection services)
        {
            services.AddWorkflowsCore();

            return services
                .AddSingleton<IWorkflowEventHandler, PersistenceWorkflowEventHandler>();
        }

        public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            return services
                .AddLogging()
                .AddLocalization()
                .AddTransient<Func<IEnumerable<IActivity>>>(sp => sp.GetServices<IActivity>)
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .TryAddProvider<ITokenFormatter, JsonTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, YamlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, XmlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionEvaluator, PlainTextEvaluator>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionEvaluator, JavaScriptEvaluator>(ServiceLifetime.Singleton)
                .AddSingleton<IScriptEngineConfigurator, CommonScriptEngineConfigurator>()
                .AddSingleton<IScriptEngineConfigurator, DateTimeScriptEngineConfigurator>()
                .AddSingleton<IWorkflowInvoker, WorkflowInvoker>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddSingleton<IActivityInvoker, ActivityInvoker>()
                .AddSingleton<IActivityResolver, ActivityResolver>()
                .AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>()
                .AddSingleton<IWorkflowSerializerProvider, WorkflowSerializerProvider>()
                .AddSingleton<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IWorkflowPublisher, WorkflowPublisher>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddStartupTask<PopulateRegistryTask>()
                .AddSingleton<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddAutoMapperProfile<WorkflowDefinitionProfile>(ServiceLifetime.Singleton)
                .AddPrimitiveActivities()
                .AddControlFlowActivities()
                .AddWorkflowActivities();
        }

        public static IServiceCollection AddActivity<T>(this IServiceCollection services)
            where T : class, IActivity
        {
            return services
                .AddTransient<T>()
                .AddTransient<IActivity>(sp => sp.GetRequiredService<T>());
        }

        /// <summary>
        /// Registers the specified service only if none already exists for the specified provider type.
        /// </summary>
        public static IServiceCollection TryAddProvider<TService, TProvider>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
        {
            return services.TryAddProvider(typeof(TService), typeof(TProvider), lifetime);
        }

        /// <summary>
        /// Registers the specified service only if none already exists for the specified provider type.
        /// </summary>
        public static IServiceCollection TryAddProvider(
            this IServiceCollection services,
            Type serviceType,
            Type providerType, ServiceLifetime lifetime)
        {
            var descriptor = services.FirstOrDefault(
                x => x.ServiceType == serviceType && x.ImplementationType == providerType
            );

            if (descriptor == null)
            {
                descriptor = new ServiceDescriptor(serviceType, providerType, lifetime);
                services.Add(descriptor);
            }

            return services;
        }

        private static IServiceCollection AddPrimitiveActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<SetVariable>()
                .AddActivity<Correlate>()
                .AddActivity<SignalEvent>()
                .AddActivity<Finish>();
        }

        private static IServiceCollection AddControlFlowActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<ForEach>()
                .AddActivity<Fork>()
                .AddActivity<Join>()
                .AddSingleton<IWorkflowEventHandler>(sp => sp.GetRequiredService<Join>())
                .AddActivity<IfElse>()
                .AddActivity<Switch>();
        }

        private static IServiceCollection AddWorkflowActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<TriggerWorkflow>();
        }
    }
}