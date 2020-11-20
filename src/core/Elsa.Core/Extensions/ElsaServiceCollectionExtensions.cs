using System;
using System.Collections.Generic;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Services;
using Elsa.Activities.Workflows;
using Elsa.Builders;
using Elsa.Consumers;
using Elsa.Converters;
using Elsa.Data.Extensions;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Indexes;
using Elsa.Mapping;
using Elsa.Metadata;
using Elsa.Metadata.Handlers;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.StartupTasks;
using Elsa.Triggers;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Rebus.ServiceProvider;
using RunWorkflow = Elsa.Messages.RunWorkflow;

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
                .AddSingleton(options.StorageFactory)
                .AddPersistence(options.ConfigurePersistence);

            options.AddWorkflowsCore();
            options.AddMediatR();
            options.AddAutoMapper();
            options.AddEndpoint<RunWorkflow>();

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

        public static IServiceCollection AddWorkflow(this IServiceCollection services, IWorkflow workflow)
        {
            return services
                .AddSingleton(workflow.GetType(), workflow)
                .AddTransient(sp => workflow);
        }

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
                .AddSingleton<TypeJsonConverter>()
                .TryAddProvider<IExpressionHandler, LiteralHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, VariableHandler>(ServiceLifetime.Singleton)
                .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
                .AddScoped<IWorkflowRegistry, WorkflowRegistry>()
                .AddScoped<IWorkflowScheduler, WorkflowScheduler>()
                .AddSingleton<IWorkflowSchedulerQueue, WorkflowSchedulerQueue>()
                .AddScoped<IWorkflowRunner, WorkflowRunner>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddSingleton<IActivityFactory, ActivityFactory>()
                .AddSingleton<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddScoped<IWorkflowSelector, WorkflowSelector>()
                .AddScoped<IWorkflowDefinitionManager, WorkflowDefinitionManager>()
                .AddScoped<IWorkflowInstanceManager, WorkflowInstanceManager>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<IWorkflowContextManager, WorkflowContextManager>()
                .AddIndexProvider<WorkflowDefinitionIndexProvider>()
                .AddIndexProvider<WorkflowInstanceIndexProvider>()
                .AddStartupRunner()
                .AddScoped<IActivityActivator, ActivityActivator>()
                .AddWorkflowProvider<ProgrammaticWorkflowProvider>()
                .AddWorkflowProvider<StorageWorkflowProvider>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<ICompositeActivityBuilder, CompositeActivityBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddAutoMapperProfile<NodaTimeProfile>()
                .AddAutoMapperProfile<CloningProfile>()
                .AddSingleton<ICloner, AutoMapperCloner>()
                .AddNotificationHandlers(typeof(ElsaServiceCollectionExtensions))
                .AddStartupTask<StartServiceBusTask>()
                .AddSingleton<ServiceBusFactory>()
                .AddSingleton<IServiceBusFactory, ServiceBusFactory>()
                .AddSingleton<ICommandSender, CommandSender>(CreateCommandSender)
                .AddSingleton<IEventPublisher, EventPublisher>(CreateEventPublisher)
                .AutoRegisterHandlersFromAssemblyOf<RunWorkflowConsumer>()
                .AddMetadataHandlers()
                .AddCoreActivities();

            return configuration;
        }

        private static CommandSender CreateCommandSender(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<ElsaOptions>();
            var messageTypes = options.MessageTypes;

            foreach (var messageType in messageTypes)
            {
                options.CreateServiceBus(messageType.Name, (bus, sp) =>
                {
                    var context = new ServiceBusEndpointConfigurationContext(bus, messageType.Name, messageType, sp);
                    options.ConfigureServiceBusEndpoint(context);
                    return bus;
                }, serviceProvider);
            }

            return ActivatorUtilities.CreateInstance<CommandSender>(serviceProvider);
        }

        private static EventPublisher CreateEventPublisher(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<ElsaOptions>();

            options.CreateServiceBus("elsa:publisher", (bus, sp) =>
            {
                var context = new ServiceBusPublishEndpointConfigurationContext(bus, "elsa:publisher", sp);
                options.ConfigureServiceBusPublishEndpoint(context);
                return bus;
            }, serviceProvider);

            return ActivatorUtilities.CreateInstance<EventPublisher>(serviceProvider);
        }

        private static IServiceCollection AddMetadataHandlers(this IServiceCollection services) =>
            services
                .AddSingleton<IActivityPropertyOptionsProvider, SelectOptionsProvider>();

        private static IServiceCollection AddCoreActivities(this IServiceCollection services) =>
            services
                .AddActivity<CompositeActivity>()
                .AddActivity<Inline>()
                .AddActivity<Finish>()
                .AddActivity<For>()
                .AddActivity<ForEach>()
                .AddActivity<ParallelForEach>()
                .AddActivity<Fork>()
                .AddActivity<IfElse>()
                .AddActivity<Join>()
                .AddActivity<Switch>()
                .AddActivity<While>()
                .AddActivity<Correlate>()
                .AddActivity<SetVariable>()
                .AddActivity<ReceiveSignal>()
                .AddTriggerProvider<ReceiveSignalTriggerProvider>()
                .AddActivity<SendSignal>()
                .AddScoped<ISignaler, Signaler>()
                .AddActivity<Elsa.Activities.Workflows.RunWorkflow>()
                .AddTriggerProvider<RunWorkflowTriggerProvider>();
    }
}