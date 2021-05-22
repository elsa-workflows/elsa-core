using System;
using System.ComponentModel;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Services;
using Elsa.Activities.Workflows;
using Elsa.ActivityTypeProviders;
using Elsa.Bookmarks;
using Elsa.Builders;
using Elsa.Consumers;
using Elsa.Converters;
using Elsa.Decorators;
using Elsa.Design;
using Elsa.Dispatch;
using Elsa.Dispatch.Consumers;
using Elsa.Events;
using Elsa.Expressions;
using Elsa.Handlers;
using Elsa.HostedServices;
using Elsa.Mapping;
using Elsa.Metadata;
using Elsa.Persistence;
using Elsa.Persistence.Decorators;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Serialization.Converters;
using Elsa.Services;
using Elsa.StartupTasks;
using Elsa.Triggers;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using NodaTime;
using Rebus.Handlers;
using BackgroundWorker = Elsa.Services.BackgroundWorker;
using IDistributedLockProvider = Elsa.Services.IDistributedLockProvider;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        static ElsaServiceCollectionExtensions()
        {
            TypeDescriptor.AddAttributes(typeof(Type), new TypeConverterAttribute(typeof(TypeTypeConverter)));
        }
        
        public static IServiceCollection AddElsaCore(
            this IServiceCollection services,
            Action<ElsaOptionsBuilder>? configure = default)
        {
            var optionsBuilder = new ElsaOptionsBuilder(services);
            configure?.Invoke(optionsBuilder);
            optionsBuilder.AddAutoMapper();

            var options = optionsBuilder.ElsaOptions;

            services
                .AddSingleton(options)
                .AddScoped(options.WorkflowDefinitionStoreFactory)
                .AddScoped(options.WorkflowInstanceStoreFactory)
                .AddScoped(options.WorkflowExecutionLogStoreFactory)
                .AddScoped(options.WorkflowTriggerStoreFactory)
                .AddSingleton(options.DistributedLockingOptions.DistributedLockProviderFactory)
                .AddSingleton(options.StorageFactory)
                .AddSingleton(options.WorkflowDefinitionDispatcherFactory)
                .AddSingleton(options.WorkflowInstanceDispatcherFactory)
                .AddSingleton(options.CorrelatingWorkflowDispatcherFactory)
                .AddSingleton<IDistributedLockProvider, DistributedLockProvider>()
                .AddStartupTask<ContinueRunningWorkflows>()
                .AddStartupTask<CreateSubscriptions>()
                .AddStartupTask<IndexTriggers>();

            optionsBuilder
                .AddWorkflowsCore()
                .AddCoreActivities();

            services.Decorate<IWorkflowDefinitionStore, InitializingWorkflowDefinitionStore>();
            services.Decorate<IWorkflowDefinitionStore, EventPublishingWorkflowDefinitionStore>();
            services.Decorate<IWorkflowInstanceStore, EventPublishingWorkflowInstanceStore>();
            services.Decorate<IWorkflowInstanceExecutor, LockingWorkflowInstanceExecutor>();

            return services;
        }

        /// <summary>
        /// Starts the specified workflow upon application startup.
        /// </summary>
        public static IServiceCollection StartWorkflow<T>(this ElsaOptionsBuilder elsaOptions) where T : class, IWorkflow
        {
            elsaOptions.AddWorkflow<T>();
            return elsaOptions.Services.AddHostedService<StartWorkflow<T>>();
        }

        /// <summary>
        /// Registers a consumer and associated message type using the competing consumer pattern. With the competing consumer pattern, only the first consumer on a given node to obtain a message will handle that message.
        /// This is in contrast to the Publisher-Subscriber pattern, where a message will be delivered to the consumer on all nodes in a cluster. To register a Publisher-Subscriber consumer, use <seealso cref="AddPubSubConsumer{TConsumer,TMessage}"/>
        /// </summary>
        /// <param name="elsaOptions"></param>
        /// <typeparam name="TConsumer"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        public static ElsaOptionsBuilder AddCompetingConsumer<TConsumer, TMessage>(this ElsaOptionsBuilder elsaOptions) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.Services.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            elsaOptions.AddCompetingMessageType<TMessage>();
            return elsaOptions;
        }
        
        public static ElsaOptionsBuilder AddPubSubConsumer<TConsumer, TMessage>(this ElsaOptionsBuilder elsaOptions) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.Services.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            elsaOptions.AddPubSubMessageType<TMessage>();
            return elsaOptions;
        }

        public static IServiceCollection AddActivityPropertyOptionsProvider<T>(this IServiceCollection services) where T : class, IActivityPropertyOptionsProvider => services.AddSingleton<IActivityPropertyOptionsProvider, T>();
        public static IServiceCollection AddRuntimeSelectItemsProvider<T>(this IServiceCollection services) where T : class, IRuntimeSelectListItemsProvider => services.AddScoped<IRuntimeSelectListItemsProvider, T>();
        public static IServiceCollection AddActivityTypeProvider<T>(this IServiceCollection services) where T : class, IActivityTypeProvider => services.AddSingleton<IActivityTypeProvider, T>();

        private static ElsaOptionsBuilder AddWorkflowsCore(this ElsaOptionsBuilder options)
        {
            var services = options.Services;

            services
                .TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddSingleton<IContainerNameAccessor, OptionsContainerNameAccessor>()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddScoped<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IActivityActivator, ActivityActivator>()
                .AddSingleton<ITokenService, TokenService>()
                .AddScoped<IWorkflowRunner, WorkflowRunner>()
                .AddScoped<WorkflowStarter>()
                .AddScoped<WorkflowResumer>()
                .AddScoped<IStartsWorkflow>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IStartsWorkflows>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IFindsAndStartsWorkflows>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IBuildsAndStartsWorkflow>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IFindsAndResumesWorkflows>(sp => sp.GetRequiredService<WorkflowResumer>())
                .AddScoped<IResumesWorkflow>(sp => sp.GetRequiredService<WorkflowResumer>())
                .AddScoped<IResumesWorkflows>(sp => sp.GetRequiredService<WorkflowResumer>())
                .AddScoped<IBuildsAndResumesWorkflow>(sp => sp.GetRequiredService<WorkflowResumer>())
                .AddScoped<IWorkflowLaunchpad, WorkflowLaunchpad>()
                .AddScoped<IWorkflowInstanceExecutor, WorkflowInstanceExecutor>()
                .AddScoped<IWorkflowTriggerInterruptor, WorkflowTriggerInterruptor>()
                .AddScoped<IWorkflowReviver, WorkflowReviver>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddTransient<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddSingleton<IWorkflowBlueprintReflector, WorkflowBlueprintReflector>()
                .AddSingleton<IBackgroundWorker, BackgroundWorker>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<IWorkflowContextManager, WorkflowContextManager>()
                .AddTransient<IActivityTypeService, ActivityTypeService>()
                .AddActivityTypeProvider<TypeBasedActivityProvider>()
                .AddScoped<IWorkflowExecutionLog, WorkflowExecutionLog>()
                .AddTransient<ICreatesWorkflowExecutionContextForWorkflowBlueprint, WorkflowExecutionContextForWorkflowBlueprintFactory>()
                .AddTransient<ICreatesActivityExecutionContextForActivityBlueprint, ActivityExecutionContextForActivityBlueprintFactory>()
                .AddTransient<IGetsStartActivities, GetsStartActivitiesProvider>()
                ;

            // Serialization.
            services
                .AddTransient<Func<JsonSerializer>>(sp => sp.GetRequiredService<JsonSerializer>)
                .AddTransient(sp => sp.GetRequiredService<ElsaOptions>().CreateJsonSerializer(sp))
                .AddSingleton<IContentSerializer, DefaultContentSerializer>()
                .AddSingleton<TypeJsonConverter>();

            // Expressions.
            services
                .TryAddProvider<IExpressionHandler, LiteralHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, VariableHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, JsonHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, SwitchHandler>(ServiceLifetime.Singleton)
                .AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

            // Workflow providers.
            services
                .AddWorkflowProvider<ProgrammaticWorkflowProvider>()
                .AddWorkflowProvider<StorageWorkflowProvider>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>();

            // Metadata.
            services
                .AddSingleton<IDescribesActivityType, TypedActivityTypeDescriber>()
                .AddSingleton<IActivityPropertyOptionsResolver, ActivityPropertyOptionsResolver>()
                .AddSingleton<IActivityPropertyDefaultValueResolver, ActivityPropertyDefaultValueResolver>()
                .AddSingleton<IActivityPropertyUIHintResolver, ActivityPropertyUIHintResolver>();

            // Bookmarks.
            services
                .AddSingleton<IBookmarkHasher, BookmarkHasher>()
                .AddScoped<IBookmarkIndexer, BookmarkIndexer>()
                .AddScoped<IBookmarkFinder, BookmarkFinder>()
                .AddScoped<ITriggerIndexer, TriggerIndexer>()
                .AddScoped<IGetsTriggersForWorkflowBlueprints, TriggersForBlueprintsProvider>()
                .AddTransient<IGetsTriggersForActivityBlueprintAndWorkflow, TriggersForActivityBlueprintAndWorkflowProvider>()
                .AddSingleton<ITriggerStore, TriggerStore>()
                .AddScoped<ITriggerFinder, TriggerFinder>()
                .AddBookmarkProvider<SignalReceivedBookmarkProvider>()
                .AddBookmarkProvider<RunWorkflowBookmarkProvider>();

            // Mediator.
            services
                .AddMediatR(mediatr => mediatr.AsScoped(), typeof(IActivity), typeof(LogWorkflowExecution));

            // Service Bus.
            services
                .AddSingleton<ServiceBusFactory>()
                .AddSingleton<IServiceBusFactory, ServiceBusFactory>()
                .AddSingleton<ICommandSender, CommandSender>()
                .AddSingleton<IEventPublisher, EventPublisher>();

            options
                .AddCompetingConsumer<TriggerWorkflowsRequestConsumer, TriggerWorkflowsRequest>()
                .AddCompetingConsumer<ExecuteWorkflowDefinitionRequestConsumer, ExecuteWorkflowDefinitionRequest>()
                .AddCompetingConsumer<ExecuteWorkflowInstanceRequestConsumer, ExecuteWorkflowInstanceRequest>()
                .AddCompetingConsumer<UpdateWorkflowTriggersIndexConsumer, WorkflowDefinitionPublished>()
                .AddCompetingConsumer<UpdateWorkflowTriggersIndexConsumer, WorkflowDefinitionRetracted>();

            // AutoMapper.
            services
                .AddAutoMapperProfile<NodaTimeProfile>()
                .AddAutoMapperProfile<CloningProfile>()
                .AddAutoMapperProfile<ExceptionProfile>()
                .AddTransient<ExceptionConverter>()
                .AddSingleton<ICloner, AutoMapperCloner>();

            // Caching.
            services
                .AddMemoryCache()
                .AddScoped<ISignaler, Signaler>()
                .Decorate<IWorkflowRegistry, CachingWorkflowRegistry>();

            // Builder API.
            services
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<ICompositeActivityBuilder, CompositeActivityBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>);

            return options;
        }

        private static ElsaOptionsBuilder AddCoreActivities(this ElsaOptionsBuilder services)
        {
            if (!services.WithCoreActivities)
                return services;

            return services
                .AddActivitiesFrom<ElsaOptions>()
                .AddActivitiesFrom<CompositeActivity>();
        }
    }
}