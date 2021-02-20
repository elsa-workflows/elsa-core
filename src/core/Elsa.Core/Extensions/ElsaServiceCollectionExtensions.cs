using System;
using Elsa;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Services;
using Elsa.Activities.Workflows;
using Elsa.ActivityProviders;
using Elsa.ActivityTypeProviders;
using Elsa.Bookmarks;
using Elsa.Builders;
using Elsa.Consumers;
using Elsa.Decorators;
using Elsa.Expressions;
using Elsa.Handlers;
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
                .AddStartupTask<ContinueRunningWorkflows>()
                .AddStartupTask<IndexTriggers>();

            options
                .AddWorkflowsCore()
                .AddCoreActivities();

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
                .AddScoped<IWorkflowTriggerInterruptor, WorkflowTriggerInterruptor>()
                .AddScoped<IWorkflowReviver, WorkflowReviver>()
                .AddSingleton<IWorkflowFactory, WorkflowFactory>()
                .AddSingleton<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddSingleton<IWorkflowBlueprintReflector, WorkflowBlueprintReflector>()
                .AddSingleton<IBackgroundWorker, BackgroundWorker>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<IWorkflowContextManager, WorkflowContextManager>()
                .AddSingleton<IActivityTypeService, ActivityTypeService>()
                .AddSingleton<IActivityTypeProvider, TypeBasedActivityProvider>()
                .AddScoped<IWorkflowExecutionLog, WorkflowExecutionLog>()
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
                .AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

            // Workflow providers.
            services
                .AddWorkflowProvider<ProgrammaticWorkflowProvider>()
                .AddWorkflowProvider<StorageWorkflowProvider>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>();

            // Metadata.
            services
                .AddSingleton<IActivityDescriber, ActivityDescriber>()
                .AddMetadataHandlers();

            // Bookmarks.
            services
                .AddSingleton<IBookmarkHasher, BookmarkHasher>()
                .AddScoped<IBookmarkIndexer, BookmarkIndexer>()
                .AddScoped<IBookmarkFinder, BookmarkFinder>()
                .AddScoped<ITriggerIndexer, TriggerIndexer>()
                .AddSingleton<ITriggerStore, TriggerStore>()
                .AddScoped<ITriggerFinder, TriggerFinder>()
                .AddBookmarkProvider<SignalReceivedBookmarkProvider>()
                .AddBookmarkProvider<RunWorkflowBookmarkProvider>();

            // Mediator.
            services
                .AddMediatR(mediatr => mediatr.AsScoped(), typeof(IActivity), typeof(LogWorkflowExecution));

            // Service Bus.
            services
                .AddScoped<IWorkflowQueue, WorkflowQueue>()
                .AddSingleton<ServiceBusFactory>()
                .AddSingleton<IServiceBusFactory, ServiceBusFactory>()
                .AddSingleton<ICommandSender, CommandSender>()
                .AddSingleton<IEventPublisher, EventPublisher>();

            options
                .AddConsumer<RunWorkflowDefinitionConsumer, RunWorkflowDefinition>()
                .AddConsumer<RunWorkflowInstanceConsumer, RunWorkflowInstance>();

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

        private static IServiceCollection AddMetadataHandlers(this IServiceCollection services) =>
            services
                .AddSingleton<IActivityPropertyOptionsProvider, SelectOptionsProvider>();

        private static ElsaOptions AddCoreActivities(this ElsaOptions services) => services.AddActivitiesFrom<ElsaOptions>();
    }
}