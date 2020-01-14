using System;
using System.Collections.Generic;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Signaling;
using Elsa.AutoMapper.Extensions;
using Elsa.Builders;
using Elsa.Caching;
using Elsa.Converters;
using Elsa.Expressions;
using Elsa.Mapping;
using Elsa.Messages;
using Elsa.Messages.Distributed;
using Elsa.Messages.Distributed.Handlers;
using Elsa.Persistence;
using Elsa.Persistence.Memory;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Serialization.Handlers;
using Elsa.Services;
using Elsa.StartupTasks;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Messages.Control;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Rebus.Transport.InMem;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaCore(
            this IServiceCollection services,
            Action<ElsaOptions> configure = null)
        {
            var configuration = new ElsaOptions(services);
            configuration.AddWorkflowsCore();
            configuration.AddMediatR();
            configure?.Invoke(configuration);
            EnsurePersistence(configuration);
            EnsureCaching(configuration);
            EnsureLockProvider(configuration);
            EnsureServiceBus(configuration);

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

        public static IServiceCollection AddTypeNameValueHandler<T>(this IServiceCollection services) where T : class, IValueHandler => services.AddTransient<IValueHandler, T>();
        public static IServiceCollection AddTypeAlias<T>(this IServiceCollection services, string alias) => services.AddTypeAlias(typeof(T), alias);
        public static IServiceCollection AddTypeAlias(this IServiceCollection services, Type type, string alias) => services.AddTransient<ITypeAlias>(sp => new TypeAlias(type, alias));
        public static IServiceCollection AddConsumer<TMessage, TConsumer>(this IServiceCollection services) where TConsumer : class, IHandleMessages<TMessage> => services.AddTransient<IHandleMessages<TMessage>, TConsumer>();  
        
        private static IServiceCollection AddMediatR(this ElsaOptions configuration)
        {
            return configuration.Services.AddMediatR(
                mediatr => mediatr.AsSingleton(),
                typeof(ElsaServiceCollectionExtensions));
        }

        private static ElsaOptions AddWorkflowsCore(this ElsaOptions configuration)
        {
            var services = configuration.Services;
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddTransient<Func<IEnumerable<IActivity>>>(sp => sp.GetServices<IActivity>)
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .AddSingleton<VariableConverter>()
                .AddSingleton<ActivityConverter>()
                .AddSingleton<TypeConverter>()
                .AddSingleton<ITypeMap, TypeMap>()
                .TryAddProvider<ITokenFormatter, JsonTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, YamlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, XmlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, LiteralHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, CodeHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, VariableHandler>(ServiceLifetime.Singleton)
                .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
                .AddSingleton<IWorkflowSerializerProvider, WorkflowSerializerProvider>()
                .AddTransient<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IWorkflowScheduler, WorkflowScheduler>()
                .AddScoped<IWorkflowHost, WorkflowHost>()
                .AddStartupRunner()
                .AddScoped<IActivityResolver, ActivityResolver>()
                .AddTransient<IWorkflowProvider, StoreWorkflowProvider>()
                .AddTransient<IWorkflowProvider, CodeWorkflowProvider>()
                .AddTransient<WorkflowBuilder>()
                .AddTransient<Func<WorkflowBuilder>>(sp => sp.GetRequiredService<WorkflowBuilder>)
                .AddAutoMapperProfile<WorkflowDefinitionProfile>(ServiceLifetime.Singleton)
                .AddSerializationHandlers()
                .AddPrimitiveActivities();

            return configuration;
        }
        
        private static IServiceCollection AddSerializationHandlers(this IServiceCollection services) =>
            services
                .AddTypeNameValueHandler<AnnualDateHandler>()
                .AddTypeNameValueHandler<DateTimeHandler>()
                .AddTypeNameValueHandler<DefaultValueHandler>()
                .AddTypeNameValueHandler<DurationHandler>()
                .AddTypeNameValueHandler<InstantHandler>()
                .AddTypeNameValueHandler<LocalDateHandler>()
                .AddTypeNameValueHandler<LocalDateTimeHandler>()
                .AddTypeNameValueHandler<LocalTimeHandler>()
                .AddTypeNameValueHandler<ObjectHandler>()
                .AddTypeNameValueHandler<ArrayHandler>()
                .AddTypeNameValueHandler<OffsetDateHandler>()
                .AddTypeNameValueHandler<OffsetHandler>()
                .AddTypeNameValueHandler<OffsetTimeHandler>()
                .AddTypeNameValueHandler<YearMonthHandler>()
                .AddTypeNameValueHandler<ZonedDateTimeHandler>()
                .AddTypeNameValueHandler<ActivityHandler>()
                .AddTypeAlias<object>("Object")
                .AddTypeAlias<bool>("Boolean")
                .AddTypeAlias<int>("Int32")
                .AddTypeAlias<long>("Int64")
                .AddTypeAlias<decimal>("Decimal")
                .AddTypeAlias<string>("String")
                .AddTypeAlias<IActivity>("Activity")
                .AddTypeAlias(typeof(IList<>), "List")
                .AddTypeAlias(typeof(ICollection<>), "Collection")
                .AddTypeAlias(typeof(LiteralExpression<>), "LiteralExpression");

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

        private static void EnsurePersistence(ElsaOptions configuration)
        {
            var hasDefinitionStore = configuration.HasService<IWorkflowDefinitionStore>();
            var hasInstanceStore = configuration.HasService<IWorkflowInstanceStore>();

            if (!hasDefinitionStore || !hasInstanceStore)
                configuration.WithMemoryStores();

            configuration.Services.Decorate<IWorkflowDefinitionStore, PublishingWorkflowDefinitionStore>();
        }

        private static void EnsureCaching(ElsaOptions configuration)
        {
            if (!configuration.HasService<ISignal>())
                configuration.Services.AddSingleton<ISignal, Signal>();

            configuration.Services.AddMemoryCache();
        }

        private static void EnsureLockProvider(ElsaOptions configuration)
        {
            if (!configuration.HasService<IDistributedLockProvider>())
                configuration.Services.AddSingleton<IDistributedLockProvider, DefaultLockProvider>();
        }

        private static void EnsureServiceBus(ElsaOptions configuration)
        {
            if (!configuration.HasService<IBus>())
            {
                configuration.WithServiceBus(rebus => rebus
                    .Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()))
                    .Routing(r => r.TypeBased())
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages")));
            }

            configuration.Services.AddStartupTask<StartServiceBusTask>();
            configuration.Services.AddConsumer<RunWorkflow, RunWorkflowHandler>();
        }
    }
}