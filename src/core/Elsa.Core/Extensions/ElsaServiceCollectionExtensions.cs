using System;
using System.Collections.Generic;
using System.Linq;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Signaling;
using Elsa.Builders;
using Elsa.Consumers;
using Elsa.Converters;
using Elsa.Data;
using Elsa.Data.Extensions;
using Elsa.Events;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Handlers;
using Elsa.Indexes;
using Elsa.Mapping;
using Elsa.Messages;
using Elsa.Metadata;
using Elsa.Metadata.Handlers;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.ServiceBus;
using Elsa.Services;
using Elsa.StartupTasks;
using Elsa.Triggers;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Rebus.Handlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaCore(
            this IServiceCollection services,
            Action<ElsaOptions>? configure = default)
        {
            var options = new ElsaOptions(services);
            configure?.Invoke(options);

            services
                .AddSingleton(options)
                .AddSingleton(options.DistributedLockProviderFactory)
                .AddSingleton(options.SignalFactory)
                .AddPersistence(options.ConfigurePersistence);

            options.AddWorkflowsCore();
            options.AddMediatR();
            options.AddServiceBus();
            options.AddAutoMapper();

            return services;
        }

        public static IServiceCollection AddActivity<T>(this IServiceCollection services)
            where T : class, IActivity
        {
            return services
                .AddTransient<T>()
                .AddTransient<IActivity>(sp => sp.GetRequiredService<T>());
        }

        public static IServiceCollection AddWorkflow<T>(this IServiceCollection services) where T : class, IWorkflow
        {
            return services
                .AddTransient<T>()
                .AddTransient<IWorkflow>(sp => sp.GetRequiredService<T>());
        }

        public static IServiceCollection AddConsumer<TMessage, TConsumer>(this IServiceCollection services) where TConsumer : class, IHandleMessages<TMessage> => services.AddTransient<IHandleMessages<TMessage>, TConsumer>();
        private static IServiceCollection AddMediatR(this ElsaOptions options) => options.Services.AddMediatR(mediatr => mediatr.AsScoped(), typeof(IActivity));

        private static ElsaOptions AddWorkflowsCore(this ElsaOptions configuration)
        {
            var services = configuration.Services;
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddMemoryCache()
                .AddTransient<Func<IEnumerable<IActivity>>>(sp => sp.GetServices<IActivity>)
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton(sp => sp.GetRequiredService<ElsaOptions>().CreateJsonSerializer(sp))
                .AddSingleton<IContentSerializer, DefaultContentSerializer>()
                .AddSingleton<TypeConverter>()
                .TryAddProvider<IExpressionHandler, LiteralHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, VariableHandler>(ServiceLifetime.Singleton)
                .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
                .AddScoped<IWorkflowRegistry, WorkflowRegistry>()
                .AddScoped<IWorkflowScheduler, WorkflowScheduler>()
                .AddSingleton<IWorkflowSchedulerQueue, WorkflowSchedulerQueue>()
                .AddScoped<IWorkflowHost, WorkflowHost>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddSingleton<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddScoped<IWorkflowSelector, WorkflowSelector>()
                .AddScoped<IWorkflowDefinitionManager, WorkflowDefinitionManager>()
                .AddScoped<IWorkflowInstanceManager, WorkflowInstanceManager>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddIndexProvider<WorkflowDefinitionIndexProvider>()
                .AddIndexProvider<WorkflowInstanceIndexProvider>()
                .AddStartupRunner()
                .AddSingleton<IActivityActivator, ActivityActivator>()
                .AddWorkflowProvider<CodeWorkflowProvider>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddAutoMapperProfile<NodaTimeProfile>()
                .AddAutoMapperProfile<CloningProfile>()
                .AddSingleton<ICloner, AutoMapperCloner>()
                .AddNotificationHandlers(typeof(ElsaServiceCollectionExtensions))
                .AddStartupTask<StartServiceBusTask>()
                .AddConsumer<RunWorkflow, RunWorkflowConsumer>()
                .AddMetadataHandlers()
                .AddPrimitiveActivities();

            return configuration;
        }

        private static IServiceCollection AddMetadataHandlers(this IServiceCollection services) =>
            services
                .AddSingleton<IActivityPropertyOptionsProvider, SelectOptionsProvider>();

        private static IServiceCollection AddPrimitiveActivities(this IServiceCollection services) =>
            services
                .AddActivity<Complete>()
                .AddActivity<For>()
                .AddActivity<ForEach>()
                .AddActivity<Fork>()
                .AddActivity<IfElse>()
                .AddActivity<Join>()
                .AddActivity<Switch>()
                .AddActivity<While>()
                .AddActivity<Correlate>()
                .AddActivity<SetVariable>()
                .AddActivity<Signaled>()
                .AddActivity<TriggerEvent>()
                .AddActivity<TriggerSignal>();

        private static ElsaOptions AddServiceBus(this ElsaOptions options)
        {
            options.WithServiceBus(options.ServiceBusConfigurer);
            return options;
        }
    }
}