using System;
using System.Linq;
using System.Reflection;
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
using Elsa.Converters;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.HostedServices;
using Elsa.Mapping;
using Elsa.Metadata;
using Elsa.Metadata.Handlers;
using Elsa.Persistence;
using Elsa.Persistence.Decorators;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Triggers;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Rebus.ServiceProvider;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaCore(
            this IServiceCollection services,
            Action<ElsaConfiguration>? configure = default)
        {
            var options = new ElsaConfiguration(services);
            configure?.Invoke(options);

            services
                .AddSingleton(options)
                .AddScoped(options.WorkflowDefinitionStoreFactory)
                .AddScoped(options.WorkflowInstanceStoreFactory)
                .AddScoped(options.WorkflowExecutionLogStoreFactory)
                .AddSingleton(options.DistributedLockProviderFactory)
                .AddSingleton(options.SignalFactory)
                .AddSingleton(options.StorageFactory);

            options.AddWorkflowsCore();
            options.AddMediatR();
            options.AddAutoMapper();

            services.Decorate<IWorkflowDefinitionStore, InitializingWorkflowDefinitionStore>();
            services.Decorate<IWorkflowInstanceStore, EventPublishingWorkflowInstanceStore>();

            return services;
        }

        public static IServiceCollection AddActivity<T>(this IServiceCollection services) where T : class, IActivity =>
            services.AddActivity(typeof(T));

        public static IServiceCollection AddActivity(this IServiceCollection services, Type activityType) =>
           services.Configure<ElsaOptions>(options => options.RegisterActivity(activityType));

        /// <summary>
        /// Add all activities (<see cref="IActivity"/>) that are in the assembly
        /// </summary>
        public static IServiceCollection AddActivity(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly.GetAllWithInterface<IActivity>();

            foreach (var type in types)
            {
                services.AddActivity(type);
            }

            return services;
        }

        public static IServiceCollection AddWorkflow<T>(this IServiceCollection services) where T : class, IWorkflow =>
            services.AddWorkflow(typeof(T));

        public static IServiceCollection AddWorkflow(this IServiceCollection services, IWorkflow workflow)
        {
            var type = workflow.GetType();
            return services
              .AddSingleton(type)
              .AddWorkflow(type);
        }

        public static IServiceCollection AddWorkflow<T>(this IServiceCollection services, Func<IServiceProvider, object> factory, ServiceLifetime serviceLifetime = ServiceLifetime.Transient) where T : class, IWorkflow
        {
            services.Add(new ServiceDescriptor(typeof(T), factory, serviceLifetime));

            return services.AddWorkflow(typeof(T));
        }

        public static IServiceCollection AddWorkflow(this IServiceCollection services, Type workflow) =>
            services.Configure<ElsaOptions>(options => options.RegisterWorkflow(workflow));

        public static IServiceCollection StartWorkflow<T>(this IServiceCollection services) where T : class, IWorkflow => services.AddHostedService<StartWorkflow<T>>();

        /// <summary>
        /// Add all workflows (<see cref="IWorkflow"/>) that are in the assembly
        /// </summary>
        /// <remarks>Instantiated or workflows with a specific implementation must be added before this call.</remarks>
        public static IServiceCollection AddWorkflow(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly.GetAllWithInterface<IWorkflow>();

            foreach (var type in types)
            {
                services.AddWorkflow(type);
            }

            return services;
        }

        private static IServiceCollection AddMediatR(this ElsaConfiguration options) => options.Services.AddMediatR(mediatr => mediatr.AsScoped(), typeof(IActivity));

        private static ElsaConfiguration AddWorkflowsCore(this ElsaConfiguration configuration)
        {
            var services = configuration.Services;
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddMemoryCache()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton(sp => sp.GetRequiredService<ElsaConfiguration>().CreateJsonSerializer(sp))
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
                .AddSingleton<IActivityFactory, ActivityFactory>()
                .AddSingleton<IWorkflowBlueprintMaterializer, WorkflowBlueprintMaterializer>()
                .AddSingleton<IWorkflowBlueprintReflector, WorkflowBlueprintReflector>()
                .AddScoped<IWorkflowSelector, WorkflowSelector>()
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddScoped<IWorkflowContextManager, WorkflowContextManager>()              
                .AddStartupRunner()
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
                .AutoRegisterHandlersFromAssemblyOf<RunWorkflowConsumer>()
                .AddMetadataHandlers()
                .AddCoreActivities();

            return configuration;
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
                .AddActivity<SetContextId>()
                .AddActivity<SignalReceived>()
                .AddActivity<SendSignal>()
                .AddActivity<RunWorkflow>()
                .AddActivity<InterruptTrigger>()
                .AddScoped<ISignaler, Signaler>()
                .AddTriggerProvider<SignalReceivedTriggerProvider>()
                .AddTriggerProvider<RunWorkflowTriggerProvider>();
    }
}