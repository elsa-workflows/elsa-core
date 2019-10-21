using System;
using System.Collections.Generic;
using System.Linq;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Workflows;
using Elsa.AutoMapper.Extensions;
using Elsa.Expressions;
using Elsa.Mapping;
using Elsa.Persistence.Memory;
using Elsa.Scripting;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.WorkflowBuilders;
using Elsa.WorkflowEventHandlers;
using Elsa.WorkflowProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflows(
            this IServiceCollection services,
            Action<ServiceConfiguration> configure)
        {
            var configuration = new ServiceConfiguration(services);
            configuration.WithWorkflowsCore();
            configure(configuration);
            return services;
        }

        public static ServiceConfiguration WithAutomaticPersistence(this ServiceConfiguration configuration)
        {
            configuration.Services.AddScoped<IWorkflowEventHandler, PersistenceWorkflowEventHandler>();
            return configuration;
        }

        private static ServiceConfiguration WithWorkflowsCore(this ServiceConfiguration configuration)
        {
            var services = configuration.Services;
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddMemoryCache()
                .AddTransient<Func<IEnumerable<IActivity>>>(sp => sp.GetServices<IActivity>)
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .TryAddProvider<ITokenFormatter, JsonTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, YamlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, XmlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionEvaluator, LiteralEvaluator>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionEvaluator, JavaScriptEvaluator>(ServiceLifetime.Singleton)
                .AddSingleton<IScriptEngineConfigurator, CommonScriptEngineConfigurator>()
                .AddSingleton<IScriptEngineConfigurator, DateTimeScriptEngineConfigurator>()
                .AddScoped<IWorkflowFactory, WorkflowFactory>()
                .AddSingleton<IActivityInvoker, ActivityInvoker>()
                .AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>()
                .AddSingleton<IWorkflowSerializerProvider, WorkflowSerializerProvider>()
                .AddSingleton<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IWorkflowInvoker, WorkflowInvoker>()
                .AddScoped<IScopedWorkflowInvoker, ScopedWorkflowInvoker>()
                .AddScoped<IActivityResolver, ActivityResolver>()
                .AddScoped<IWorkflowEventHandler, ActivityLoggingWorkflowEventHandler>()
                .AddTransient<IWorkflowProvider, StoreWorkflowProvider>()
                .AddTransient<IWorkflowProvider, CodeWorkflowProvider>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddAutoMapperProfile<WorkflowDefinitionProfile>(ServiceLifetime.Singleton)
                .AddPrimitiveActivities()
                .AddControlFlowActivities()
                .AddWorkflowActivities();

            return configuration;
        }

        public static IServiceCollection AddWorkflow<T>(this IServiceCollection services)
            where T : class, IWorkflow
        {
            return services.AddTransient<IWorkflow, T>();
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
            Type providerType,
            ServiceLifetime lifetime)
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

        public static IServiceCollection Replace<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
        {
            return services.Replace(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        }

        private static IServiceCollection AddPrimitiveActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<SetVariable>();
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
                .AddActivity<TriggerWorkflow>()
                .AddActivity<Correlate>()
                .AddActivity<Signaled>()
                .AddActivity<Trigger>()
                .AddActivity<Start>()
                .AddActivity<Finish>();
        }
    }
}