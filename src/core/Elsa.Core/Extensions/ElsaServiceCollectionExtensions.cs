using System;
using Elsa;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Services;
using Elsa.Activities.Workflows;
using Elsa.ActivityProviders;
using Elsa.ActivityTypeProviders;
using Elsa.Bookmarks;
using Elsa.Builders;
using Elsa.Decorators;
using Elsa.Dispatch;
using Elsa.Dispatch.Consumers;
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
                .AddScoped(options.WorkflowDefinitionStoreFactory)
                .AddScoped(options.WorkflowInstanceStoreFactory)
                .AddScoped(options.WorkflowExecutionLogStoreFactory)
                .AddScoped(options.WorkflowTriggerStoreFactory)
                .AddSingleton(options.DistributedLockProviderFactory)
                .AddSingleton(options.SignalFactory)
                .AddSingleton(options.StorageFactory)
                .AddSingleton(options.WorkflowDefinitionDispatcherFactory)
                .AddSingleton(options.WorkflowInstanceDispatcherFactory)
                .AddSingleton(options.CorrelatingWorkflowDispatcherFactory)
                .AddStartupTask<ContinueRunningWorkflows>()
                .AddStartupTask<IndexTriggers>();

            options
                .AddWorkflowsCore()
                .AddCoreActivities();

            options.AddAutoMapper();

            services.Decorate<IWorkflowDefinitionStore, InitializingWorkflowDefinitionStore>();
            services.Decorate<IWorkflowDefinitionStore, EventPublishingWorkflowDefinitionStore>();
            services.Decorate<IWorkflowInstanceStore, EventPublishingWorkflowInstanceStore>();
            services.Decorate<IWorkflowRunner, LockingWorkflowRunner>();

            return services;
        }

        /// <summary>
        /// Starts the specified workflow upon application startup.
        /// </summary>
        public static IServiceCollection StartWorkflow<T>(this ElsaOptions elsaOptions) where T : class, IWorkflow
        {
            elsaOptions.AddWorkflow<T>();
            return elsaOptions.Services.AddHostedService<StartWorkflow<T>>();
        }

        public static ElsaOptions AddConsumer<TConsumer, TMessage>(this ElsaOptions elsaOptions) where TConsumer : class, IHandleMessages<TMessage>
        {
            elsaOptions.Services.AddTransient<IHandleMessages<TMessage>, TConsumer>();
            elsaOptions.AddMessageType<TMessage>();
            return elsaOptions;
        }

        public static IServiceCollection AddActivityPropertyOptionsProvider<T>(this IServiceCollection services) where T : class, IActivityPropertyOptionsProvider => services.AddSingleton<IActivityPropertyOptionsProvider, T>();
        public static IServiceCollection AddActivityTypeProvider<T>(this IServiceCollection services) where T : class, IActivityTypeProvider => services.AddSingleton<IActivityTypeProvider, T>();

        private static ElsaOptions AddWorkflowsCore(this ElsaOptions options)
        {
            var services = options.Services;

            services
                .TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddScoped<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IActivityActivator, ActivityActivator>()
                .AddScoped<IWorkflowRunner, WorkflowRunner>()
                .AddScoped<WorkflowStarter>()
                .AddScoped<WorkflowResumer>()
                .AddScoped<ITriggersWorkflows, TriggersWorkflows>()
                .AddScoped<IStartsWorkflow>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IStartsWorkflows>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IFindsAndStartsWorkflows>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IBuildsAndStartsWorkflow>(sp => sp.GetRequiredService<WorkflowStarter>())
                .AddScoped<IFindsAndResumesWorkflows>(sp => sp.GetRequiredService<WorkflowResumer>())
                .AddScoped<IResumesWorkflow>(sp => sp.GetRequiredService<WorkflowResumer>())
                .AddScoped<IResumesWorkflows>(sp => sp.GetRequiredService<WorkflowResumer>())
                .AddScoped<IBuildsAndResumesWorkflow>(sp => sp.GetRequiredService<WorkflowResumer>())
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
                .AddTransient<IGetsStartActivitiesForCompositeActivityBlueprint, StartActivitiesForCompositeActivityBlueprintProvider>()
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
                .AddConsumer<TriggerWorkflowsRequestConsumer, TriggerWorkflowsRequest>()
                .AddConsumer<ExecuteWorkflowDefinitionRequestConsumer, ExecuteWorkflowDefinitionRequest>()
                .AddConsumer<ExecuteWorkflowInstanceRequestConsumer, ExecuteWorkflowInstanceRequest>();

            // AutoMapper.
            services
                .AddAutoMapperProfile<NodaTimeProfile>()
                .AddAutoMapperProfile<CloningProfile>()
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

        private static ElsaOptions AddCoreActivities(this ElsaOptions services)
        {
            if (!services.WithCoreActivities)
                return services;

            return services
                .AddActivitiesFrom<ElsaOptions>()
                .AddActivitiesFrom<CompositeActivity>();
        }
    }
}