using System;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Services;
using Elsa.Activities.Workflows;
using Elsa.ActivityProviders;
using Elsa.ActivityTypeProviders;
using Elsa.Builders;
using Elsa.Consumers;
using Elsa.Expressions;
using Elsa.HostedServices;
using Elsa.Mapping;
using Elsa.Messages;
using Elsa.Metadata;
using Elsa.Metadata.Handlers;
using Elsa.Persistence;
using Elsa.Persistence.Decorators;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Serialization.Converters;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.StartupTasks;
using Elsa.Triggers;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Rebus.Handlers;
using Rebus.ServiceProvider;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaCore(
            this IServiceCollection services,
            Action<ElsaOptions>? configure = default)
        {
            services.AddStartupRunner();
            
            var options = new ElsaOptions(services);
            configure?.Invoke(options);

            services
                .AddSingleton(options)
                .AddScoped(options.WorkflowDefinitionStoreFactory)
                .AddScoped(options.WorkflowInstanceStoreFactory)
                .AddScoped(options.WorkflowExecutionLogStoreFactory)
                .AddSingleton(options.DistributedLockProviderFactory)
                .AddSingleton(options.SignalFactory)
                .AddSingleton(options.StorageFactory)
                .AddStartupTask<CreateSubscriptions>();

            options
                .AddWorkflowsCore()
                .AddCoreActivities();
            
            options.AddMediatR();
            options.AddAutoMapper();
            options.AddConsumer<RunWorkflowDefinitionConsumer, RunWorkflowDefinition>();
            options.AddConsumer<RunWorkflowInstanceConsumer, RunWorkflowInstance>();

            services.Decorate<IWorkflowDefinitionStore, InitializingWorkflowDefinitionStore>();
            services.Decorate<IWorkflowDefinitionStore, EventPublishingWorkflowDefinitionStore>();
            services.Decorate<IWorkflowInstanceStore, EventPublishingWorkflowInstanceStore>();

            return services;
        }
        
        /// <summary>
        /// Starts the specified workflow upon application startup.
        /// </summary>
        public static IServiceCollection StartWorkflow<T>(this IServiceCollection services) where T : class, IWorkflow => services.AddHostedService<StartWorkflow<T>>();
        
        public static ElsaOptions AddConsumer<TConsumer, TMessage>(this ElsaOptions elsaOptions) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.Services.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            elsaOptions.AddMessageType<TMessage>();
            return elsaOptions;
        }

        private static ElsaOptions AddMediatR(this ElsaOptions options)
        {
            options.Services.AddMediatR(mediatr => mediatr.AsScoped(), typeof(IActivity));
            return options;
        }

        private static ElsaOptions AddWorkflowsCore(this ElsaOptions options)
        {
            var services = options.Services;
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddMemoryCache()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton(sp => sp.GetRequiredService<ElsaOptions>().CreateJsonSerializer(sp))
                .AddSingleton<IContentSerializer, DefaultContentSerializer>()
                .AddSingleton<TypeJsonConverter>()
                .TryAddProvider<IExpressionHandler, LiteralHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, VariableHandler>(ServiceLifetime.Singleton)
                .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
                .AddScoped<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IActivityActivator, ActivityActivator>()
                .AddScoped<IWorkflowRunner, WorkflowRunner>()
                .AddScoped<IWorkflowTriggerInterruptor, WorkflowTriggerInterruptor>()
                .AddSingleton<IActivityDescriber, ActivityDescriber>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddSingleton<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddSingleton<IWorkflowBlueprintReflector, WorkflowBlueprintReflector>()
                .AddSingleton<IBackgroundWorker, BackgroundWorker>()
                .AddScoped<IWorkflowQueue, WorkflowQueue>()
                .AddScoped<IWorkflowSelector, WorkflowSelector>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<IWorkflowContextManager, WorkflowContextManager>()
                .AddSingleton<IActivityTypeService, ActivityTypeService>()
                .AddSingleton<IActivityTypeProvider, TypeBasedActivityProvider>()
                .AddWorkflowProvider<ProgrammaticWorkflowProvider>()
                .AddWorkflowProvider<StorageWorkflowProvider>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<ICompositeActivityBuilder, CompositeActivityBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddAutoMapperProfile<NodaTimeProfile>()
                .AddAutoMapperProfile<CloningProfile>()
                .AddSingleton<ICloner, AutoMapperCloner>()
                .AddNotificationHandlers(typeof(ElsaServiceCollectionExtensions))
                .AddSingleton<ServiceBusFactory>()
                .AddSingleton<IServiceBusFactory, ServiceBusFactory>()
                .AddSingleton<ICommandSender, CommandSender>()
                .AddSingleton<IEventPublisher, EventPublisher>()
                .AutoRegisterHandlersFromAssemblyOf<RunWorkflowInstanceConsumer>()
                .AddScoped<ISignaler, Signaler>()
                .AddTriggerProvider<SignalReceivedTriggerProvider>()
                .AddTriggerProvider<RunWorkflowTriggerProvider>()
                .AddMetadataHandlers();

            return options;
        }

        private static IServiceCollection AddMetadataHandlers(this IServiceCollection services) =>
            services
                .AddSingleton<IActivityPropertyOptionsProvider, SelectOptionsProvider>();

        private static ElsaOptions AddCoreActivities(this ElsaOptions services) =>
            services
                .AddActivity<CompositeActivity>()
                .AddActivity<Inline>()
                .AddActivity<SetName>()
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
                .AddActivity<SetTransientVariable>()
                .AddActivity<SetContextId>()
                .AddActivity<SignalReceived>()
                .AddActivity<SendSignal>()
                .AddActivity<RunWorkflow>()
                .AddActivity<InterruptTrigger>();
    }
}