using System;
using System.ComponentModel;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Services;
using Elsa.Activities.Workflows;
using Elsa.Builders;
using Elsa.Converters;
using Elsa.Decorators;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Handlers;
using Elsa.HostedServices;
using Elsa.Mapping;
using Elsa.Metadata;
using Elsa.Options;
using Elsa.Persistence;
using Elsa.Persistence.Decorators;
using Elsa.Providers.Activities;
using Elsa.Providers.ActivityTypes;
using Elsa.Providers.Workflows;
using Elsa.Providers.WorkflowStorage;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Serialization.Converters;
using Elsa.Services;
using Elsa.Services.Bookmarks;
using Elsa.Services.Compensation;
using Elsa.Services.Dispatch.Consumers;
using Elsa.Services.Locking;
using Elsa.Services.Messaging;
using Elsa.Services.Stability;
using Elsa.Services.Triggers;
using Elsa.Services.WorkflowContexts;
using Elsa.Services.Workflows;
using Elsa.Services.WorkflowStorage;
using Elsa.StartupTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using NodaTime;
using Rebus.Handlers;
using Storage.Net;
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
                .AddScoped(options.BookmarkStoreFactory)
                .AddScoped(options.TriggerStoreFactory)
                .AddSingleton(options.DistributedLockingOptions.DistributedLockProviderFactory)
                .AddScoped(options.WorkflowDefinitionDispatcherFactory)
                .AddScoped(options.WorkflowInstanceDispatcherFactory)
                .AddScoped(options.CorrelatingWorkflowDispatcherFactory)
                .AddScoped<ILoopDetectorProvider, LoopDetectorProvider>()
                .AddScoped<ILoopHandlerProvider, LoopHandlerProvider>()
                .AddScoped<ActivityExecutionCountLoopDetector>()
                .AddScoped<CooldownLoopHandler>()
                .AddSingleton<IDistributedLockProvider, DistributedLockProvider>()
                .AddStartupTask<ContinueRunningWorkflows>()
                .AddStartupTask<CreateSubscriptions>()
                .AddStartupTask<IndexTriggers>();

            optionsBuilder
                .AddWorkflowsCore()
                .AddConfiguration()
                .AddCoreActivities();

            if (options.UseTenantSignaler)
            {
                services.AddScoped<ISignaler, TenantSignaler>();
            }

            services
                .Decorate<IWorkflowDefinitionStore, InitializingWorkflowDefinitionStore>()
                .Decorate<IWorkflowDefinitionStore, EventPublishingWorkflowDefinitionStore>()
                .Decorate<IWorkflowInstanceStore, EventPublishingWorkflowInstanceStore>()
                .Decorate<IWorkflowInstanceExecutor, LockingWorkflowInstanceExecutor>()
                .Decorate<IWorkflowInstanceCanceller, LockingWorkflowInstanceCanceller>()
                .Decorate<IWorkflowInstanceDeleter, LockingWorkflowInstanceDeleter>();

            //TenantId default source
            services.TryAddScoped<ITenantAccessor, DefaultTenantAccessor>();

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
        public static ElsaOptionsBuilder AddCompetingConsumer<TConsumer, TMessage>(this ElsaOptionsBuilder elsaOptions, string? queueName = default) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.AddCompetingConsumerService<TConsumer, TMessage>();
            elsaOptions.AddCompetingMessageType<TMessage>(queueName);
            return elsaOptions;
        }

        private static ElsaOptionsBuilder AddCompetingConsumerService<TConsumer, TMessage>(this ElsaOptionsBuilder elsaOptions) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.Services.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            return elsaOptions;
        }

        public static ElsaOptionsBuilder AddPubSubConsumer<TConsumer, TMessage>(this ElsaOptionsBuilder elsaOptions, string? queueName = default) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.Services.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            elsaOptions.AddPubSubMessageType<TMessage>(queueName);
            return elsaOptions;
        }

        public static IServiceCollection AddActivityPropertyOptionsProvider<T>(this IServiceCollection services) where T : class, IActivityPropertyOptionsProvider => services.AddSingleton<IActivityPropertyOptionsProvider, T>();
        public static IServiceCollection AddRuntimeSelectItemsProvider<T>(this IServiceCollection services) where T : class, IRuntimeSelectListItemsProvider => services.AddScoped<IRuntimeSelectListItemsProvider, T>();
        public static IServiceCollection AddActivityTypeProvider<T>(this IServiceCollection services) where T : class, IActivityTypeProvider => services.AddSingleton<IActivityTypeProvider, T>();

        public static IServiceCollection AddWorkflowStorageProvider<T>(this IServiceCollection services) where T : class, IWorkflowStorageProvider =>
            services
                .AddSingleton<T>()
                .AddSingleton<IWorkflowStorageProvider>(sp => sp.GetRequiredService<T>());

        private static ElsaOptionsBuilder AddWorkflowsCore(this ElsaOptionsBuilder elsaOptions)
        {
            var services = elsaOptions.Services;

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
                .AddScoped<IWorkflowInstanceCanceller, WorkflowInstanceCanceller>()
                .AddScoped<IWorkflowInstanceDeleter, WorkflowInstanceDeleter>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddTransient<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddSingleton<IWorkflowBlueprintReflector, WorkflowBlueprintReflector>()
                .AddSingleton<IBackgroundWorker, BackgroundWorker>()
                .AddSingleton<ICompensationService, CompensationService>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<IWorkflowContextManager, WorkflowContextManager>()
                .AddScoped<IActivityTypeService, ActivityTypeService>()
                .AddActivityTypeProvider<TypeBasedActivityProvider>()
                .AddTransient<ICreatesWorkflowExecutionContextForWorkflowBlueprint, WorkflowExecutionContextForWorkflowBlueprintFactory>()
                .AddTransient<ICreatesActivityExecutionContextForActivityBlueprint, ActivityExecutionContextForActivityBlueprintFactory>()
                .AddTransient<IGetsStartActivities, GetsStartActivitiesProvider>();

            // Data Protection.
            services.AddDataProtection();

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
                .AddWorkflowProvider<BlobStorageWorkflowProvider>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>();

            services.Configure<BlobStorageWorkflowProviderOptions>(o => o.BlobStorageFactory = StorageFactory.Blobs.InMemory);

            // Workflow Storage Providers.
            services
                .AddSingleton<IWorkflowStorageService, WorkflowStorageService>()
                .AddWorkflowStorageProvider<TransientWorkflowStorageProvider>()
                .AddWorkflowStorageProvider<WorkflowInstanceWorkflowStorageProvider>()
                .AddWorkflowStorageProvider<BlobStorageWorkflowStorageProvider>();

            services.Configure<BlobStorageWorkflowStorageProviderOptions>(o => o.BlobStorageFactory = StorageFactory.Blobs.InMemory);

            // Metadata.
            services
                .AddSingleton<IDescribesActivityType, TypedActivityTypeDescriber>()
                .AddSingleton<IActivityPropertyOptionsResolver, ActivityPropertyOptionsResolver>()
                .AddSingleton<IActivityPropertyDefaultValueResolver, ActivityPropertyDefaultValueResolver>()
                .AddSingleton<IActivityPropertyUIHintResolver, ActivityPropertyUIHintResolver>();

            // Bookmarks.
            services
                .AddSingleton<IBookmarkHasher, BookmarkHasher>()
                .AddSingleton<IBookmarkSerializer, BookmarkSerializer>()
                .AddScoped<IBookmarkIndexer, BookmarkIndexer>()
                .AddScoped<IBookmarkFinder, BookmarkFinder>()
                .AddScoped<ITriggerIndexer, TriggerIndexer>()
                .AddScoped<IGetsTriggersForWorkflowBlueprints, TriggersForBlueprintsProvider>()
                .AddTransient<IGetsTriggersForActivityBlueprintAndWorkflow, TriggersForActivityBlueprintAndWorkflowProvider>()
                .AddScoped<ITriggerFinder, TriggerFinder>()
                .AddScoped<ITriggerRemover, TriggerRemover>()
                .AddBookmarkProvider<SignalReceivedBookmarkProvider>()
                .AddBookmarkProvider<RunWorkflowBookmarkProvider>();

            // Mediator.
            services.AddMediatR(cfg =>
            {
                cfg.Lifetime = ServiceLifetime.Scoped;
                cfg.RegisterServicesFromAssemblyContaining<IActivity>();
                cfg.RegisterServicesFromAssemblyContaining<LogWorkflowExecution>();
            });

            // Service Bus.
            services
                .AddSingleton<ServiceBusFactory>()
                .AddSingleton<IServiceBusFactory>(sp => sp.GetRequiredService<ServiceBusFactory>())
                .AddSingleton<ICommandSender, CommandSender>()
                .AddSingleton<IEventPublisher, EventPublisher>();

            elsaOptions
                .AddCompetingConsumer<TriggerWorkflowsRequestConsumer, TriggerWorkflowsRequest>("ExecuteWorkflow")
                .AddCompetingConsumer<ExecuteWorkflowDefinitionRequestConsumer, ExecuteWorkflowDefinitionRequest>("ExecuteWorkflow")
                .AddCompetingConsumer<ExecuteWorkflowInstanceRequestConsumer, ExecuteWorkflowInstanceRequest>("ExecuteWorkflow");

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

            return elsaOptions;
        }

        private static ElsaOptionsBuilder AddConfiguration(this ElsaOptionsBuilder options)
        {
            // When using Elsa in console apps, no configuration will be registered by default, but some Elsa services depend on this service (even when there is nothing configured).
            options.Services.TryAdd(new ServiceDescriptor(typeof(IConfiguration), _ => new ConfigurationBuilder().AddInMemoryCollection().Build(), ServiceLifetime.Singleton));

            return options;
        }

        private static ElsaOptionsBuilder AddCoreActivities(this ElsaOptionsBuilder options)
        {
            if (!options.WithCoreActivities)
                return options;

            return options
                .AddActivitiesFrom<ElsaOptions>()
                .AddActivitiesFrom<CompositeActivity>();
        }
    }
}