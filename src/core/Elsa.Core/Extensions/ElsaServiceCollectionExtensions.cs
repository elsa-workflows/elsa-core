using System;
using System.ComponentModel;
using Autofac;
using Autofac.Core;
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
using Elsa.Extensions;
using Elsa.Handlers;
using Elsa.HostedServices;
using Elsa.Mapping;
using Elsa.Metadata;
using Elsa.Multitenancy;
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
using MediatR;
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

        public static ContainerBuilder ConfigureElsaServices(
            this ContainerBuilder containerBuilder, IServiceCollection serviceCollection,
            Action<ElsaOptionsBuilder>? configure = default)
        {
            var optionsBuilder = new ElsaOptionsBuilder(containerBuilder, serviceCollection);
            configure?.Invoke(optionsBuilder);
            optionsBuilder.AddAutoMapper();

            var options = optionsBuilder.ElsaOptions;

            optionsBuilder.ContainerBuilder
                .AddSingleton(options)
                .AddScoped(options.WorkflowDefinitionStoreFactory)
                .AddScoped(options.WorkflowInstanceStoreFactory)
                .AddScoped(options.WorkflowExecutionLogStoreFactory)
                .AddScoped(options.BookmarkStoreFactory)
                .AddScoped(options.TriggerStoreFactory)
                .AddMultiton(options.DistributedLockingOptions.DistributedLockProviderFactory)
                .AddScoped(options.WorkflowDefinitionDispatcherFactory)
                .AddScoped(options.WorkflowInstanceDispatcherFactory)
                .AddScoped(options.CorrelatingWorkflowDispatcherFactory);

            if (options.UseTenantSignaler)
            {
                optionsBuilder.ContainerBuilder.AddScoped<ISignaler, TenantSignaler>();
            }

            optionsBuilder.AddCoreActivities();

            optionsBuilder.ContainerBuilder
                .AddScoped<ILoopDetectorProvider, LoopDetectorProvider>()
                .AddScoped<ILoopHandlerProvider, LoopHandlerProvider>()
                .AddScoped<ActivityExecutionCountLoopDetector>()
                .AddScoped<CooldownLoopHandler>()
                .AddMultiton<IDistributedLockProvider, DistributedLockProvider>()
                .AddStartupTask<ContinueRunningWorkflows>()
                .AddStartupTask<CreateSubscriptions>()
                .AddStartupTask<IndexTriggers>()
                .AddStartupTask<BackgroundServiceRunner>();

            optionsBuilder.ContainerBuilder
                .AddMultiton<IContainerNameAccessor, OptionsContainerNameAccessor>()
                .AddMultiton<IIdGenerator, IdGenerator>()
                .AddScoped<IWorkflowRegistry, WorkflowRegistry>()
                .AddMultiton<IActivityActivator, ActivityActivator>()
                .AddMultiton<ITokenService, TokenService>()
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
                .AddMultiton<IWorkflowFactory, WorkflowFactory>()
                .AddTransient<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddMultiton<IWorkflowBlueprintReflector, WorkflowBlueprintReflector>()
                .AddMultiton<IBackgroundWorker, BackgroundWorker>()
                .AddMultiton<ICompensationService, CompensationService>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<IWorkflowContextManager, WorkflowContextManager>()
                .AddScoped<IActivityTypeService, ActivityTypeService>()
                .AddActivityTypeProvider<TypeBasedActivityProvider>()
                .AddTransient<ICreatesWorkflowExecutionContextForWorkflowBlueprint, WorkflowExecutionContextForWorkflowBlueprintFactory>()
                .AddTransient<ICreatesActivityExecutionContextForActivityBlueprint, ActivityExecutionContextForActivityBlueprintFactory>()
                .AddTransient<IGetsStartActivities, GetsStartActivitiesProvider>();


            // Workflow providers.
            optionsBuilder.ContainerBuilder
                .AddWorkflowProvider<ProgrammaticWorkflowProvider>()
                .AddWorkflowProvider<BlobStorageWorkflowProvider>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>();

            // Bookmarks.
            optionsBuilder.ContainerBuilder
                .AddMultiton<IBookmarkHasher, BookmarkHasher>()
                .AddMultiton<IBookmarkSerializer, BookmarkSerializer>()
                .AddScoped<IBookmarkIndexer, BookmarkIndexer>()
                .AddScoped<IBookmarkFinder, BookmarkFinder>()
                .AddScoped<ITriggerIndexer, TriggerIndexer>()
                .AddScoped<IGetsTriggersForWorkflowBlueprints, TriggersForBlueprintsProvider>()
                .AddTransient<IGetsTriggersForActivityBlueprintAndWorkflow, TriggersForActivityBlueprintAndWorkflowProvider>()
                .AddScoped<ITriggerFinder, TriggerFinder>()
                .AddScoped<ITriggerRemover, TriggerRemover>()
                .AddBookmarkProvider<SignalReceivedBookmarkProvider>()
                .AddBookmarkProvider<RunWorkflowBookmarkProvider>();

            // Workflow Storage Providers.
            optionsBuilder.ContainerBuilder
                .AddMultiton<IWorkflowStorageService, WorkflowStorageService>()
                .AddWorkflowStorageProvider<TransientWorkflowStorageProvider>()
                .AddWorkflowStorageProvider<WorkflowInstanceWorkflowStorageProvider>()
                .AddWorkflowStorageProvider<BlobStorageWorkflowStorageProvider>();

            optionsBuilder.ContainerBuilder
                .Decorate<IWorkflowRegistry, CachingWorkflowRegistry>();


            // Service Bus.
            optionsBuilder.ContainerBuilder
                .AddMultiton<ServiceBusFactory>()
                .AddMultiton<IServiceBusFactory>(sp => sp.GetRequiredService<ServiceBusFactory>())
                .AddMultiton<ICommandSender, CommandSender>()
                .AddMultiton<IEventPublisher, EventPublisher>();

            optionsBuilder
                .AddCompetingConsumer<TriggerWorkflowsRequestConsumer, TriggerWorkflowsRequest>("ExecuteWorkflow")
                .AddCompetingConsumer<ExecuteWorkflowDefinitionRequestConsumer, ExecuteWorkflowDefinitionRequest>("ExecuteWorkflow")
                .AddCompetingConsumer<ExecuteWorkflowInstanceRequestConsumer, ExecuteWorkflowInstanceRequest>("ExecuteWorkflow");

            optionsBuilder.ContainerBuilder
                .Decorate<IWorkflowDefinitionStore, InitializingWorkflowDefinitionStore>()
                .Decorate<IWorkflowDefinitionStore, EventPublishingWorkflowDefinitionStore>()
                .Decorate<IWorkflowInstanceStore, EventPublishingWorkflowInstanceStore>();

            optionsBuilder.ContainerBuilder
                .Decorate<IWorkflowInstanceExecutor, LockingWorkflowInstanceExecutor>()
                .Decorate<IWorkflowInstanceCanceller, LockingWorkflowInstanceCanceller>()
                .Decorate<IWorkflowInstanceDeleter, LockingWorkflowInstanceDeleter>();

            return containerBuilder;
        }

        public static IServiceCollection AddElsaCore
            (
            this IServiceCollection services)
        {
            services.AddWorkflowsCore().AddConfiguration();

            //TenantId default source
            services.TryAddScoped<ITenantAccessor, DefaultTenantAccessor>();

            return services;
        }

        /// <summary>
        /// Starts the specified workflow upon application startup.
        /// </summary>
        public static ContainerBuilder StartWorkflow<T>(this ElsaOptionsBuilder elsaOptions) where T : class, IWorkflow
        {
            elsaOptions.AddWorkflow<T>();
            return elsaOptions.ContainerBuilder.AddHostedService<StartWorkflow<T>>();
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
            elsaOptions.ContainerBuilder.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            return elsaOptions;
        }

        public static ElsaOptionsBuilder AddPubSubConsumer<TConsumer, TMessage>(this ElsaOptionsBuilder elsaOptions, string? queueName = default) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.ContainerBuilder.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            elsaOptions.AddPubSubMessageType<TMessage>(queueName);
            return elsaOptions;
        }

        public static IServiceCollection AddActivityPropertyOptionsProvider<T>(this IServiceCollection services) where T : class, IActivityPropertyOptionsProvider => services.AddSingleton<IActivityPropertyOptionsProvider, T>();
        public static IServiceCollection AddRuntimeSelectItemsProvider<T>(this IServiceCollection services) where T : class, IRuntimeSelectListItemsProvider => services.AddScoped<IRuntimeSelectListItemsProvider, T>();

        public static ContainerBuilder AddActivityTypeProvider<T>(this ContainerBuilder services) where T : class, IActivityTypeProvider => services.AddMultiton<IActivityTypeProvider, T>();

        public static ContainerBuilder AddWorkflowStorageProvider<T>(this ContainerBuilder services) where T : class, IWorkflowStorageProvider =>
            services
                .AddMultiton<T>()
                .AddMultiton<IWorkflowStorageProvider>(sp => sp.GetRequiredService<T>());

        private static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
        {
            services
                .TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization();

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

            //// Workflow providers.

            services.Configure<BlobStorageWorkflowProviderOptions>(o => o.BlobStorageFactory = StorageFactory.Blobs.InMemory);

            //// Workflow Storage Providers.

            services.Configure<BlobStorageWorkflowStorageProviderOptions>(o => o.BlobStorageFactory = StorageFactory.Blobs.InMemory);

            // Metadata.
            services
                .AddSingleton<IDescribesActivityType, TypedActivityTypeDescriber>()
                .AddSingleton<IActivityPropertyOptionsResolver, ActivityPropertyOptionsResolver>()
                .AddSingleton<IActivityPropertyDefaultValueResolver, ActivityPropertyDefaultValueResolver>()
                .AddSingleton<IActivityPropertyUIHintResolver, ActivityPropertyUIHintResolver>();

            // Mediator.
            services
                .AddMediatR(mediatr => mediatr.AsScoped(), typeof(IActivity), typeof(LogWorkflowExecution));

            //// Service Bus.

            

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
                .AddScoped<ISignaler, Signaler>();
//                .Decorate<IWorkflowRegistry, CachingWorkflowRegistry>();

            // Builder API.
            services
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<ICompositeActivityBuilder, CompositeActivityBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>);

            return services;
        }

        public static ElsaOptionsBuilder AddElsaMultitenancy(this ElsaOptionsBuilder options)
        {
            options.Services.AddSingleton<ITenantStore, ConfigurationTenantStore>();
            return options;
        }

        private static IServiceCollection AddConfiguration(this IServiceCollection services)
        {
            // When using Elsa in console apps, no configuration will be registered by default, but some Elsa services depend on this service (even when there is nothing configured).
            services.TryAdd(new ServiceDescriptor(typeof(IConfiguration), _ => new ConfigurationBuilder().AddInMemoryCollection().Build(), ServiceLifetime.Singleton));

            return services;
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