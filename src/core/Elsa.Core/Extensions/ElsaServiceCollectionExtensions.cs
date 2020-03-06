using System;
using System.Collections.Generic;
using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Signaling;
using Elsa.AutoMapper.Extensions;
using Elsa.Builders;
using Elsa.Converters;
using Elsa.Expressions;
using Elsa.Mapping;
using Elsa.Messaging;
using Elsa.Messaging.Distributed;
using Elsa.Messaging.Distributed.Handlers;
using Elsa.Metadata;
using Elsa.Metadata.Handlers;
using Elsa.Persistence.Memory;
using Elsa.Runtime;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Serialization.Handlers;
using Elsa.Services;
using Elsa.StartupTasks;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.AspNetCore.Http;
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
            Action<ElsaOptions> configure = null)
        {
            var options = new ElsaOptions(services);
            configure?.Invoke(options);

            services
                .AddTransient(options.WorkflowDefinitionVersionStoreFactory)
                .AddTransient(options.WorkflowDefinitionStoreFactory)
                .AddTransient(options.WorkflowInstanceStoreFactory)
                .AddSingleton(options.DistributedLockProviderFactory)
                .AddSingleton(options.SignalFactory);

            options.AddWorkflowsCore();
            options.AddServiceBus();
            options.AddMediatR();

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
        public static IServiceCollection AddTypeAlias<T>(this IServiceCollection services) => services.AddTypeAlias<T>(typeof(T).Name);
        public static IServiceCollection AddTypeAlias(this IServiceCollection services, Type type, string alias) => services.AddTransient<ITypeAlias>(sp => new TypeAlias(type, alias));
        public static IServiceCollection AddConsumer<TMessage, TConsumer>(this IServiceCollection services) where TConsumer : class, IHandleMessages<TMessage> => services.AddTransient<IHandleMessages<TMessage>, TConsumer>();

        private static IServiceCollection AddMediatR(this ElsaOptions configuration)
        {
            return configuration.Services.AddMediatR(
                mediatr => mediatr.AsScoped(),
                typeof(ElsaServiceCollectionExtensions));
        }

        private static ElsaOptions AddWorkflowsCore(this ElsaOptions configuration)
        {
            var services = configuration.Services;
            services.TryAddSingleton<IClock>(SystemClock.Instance);

            services
                .AddLogging()
                .AddLocalization()
                .AddMemoryCache()
                .AddTransient<Func<IEnumerable<IActivity>>>(sp => sp.GetServices<IActivity>)
                .AddSingleton<IActivityDescriber, ActivityDescriber>()
                .AddSingleton<IIdGenerator, IdGenerator>()
                .AddSingleton<ITokenSerializerProvider, TokenSerializerProvider>()
                .AddSingleton<ITokenSerializer, TokenSerializer>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .AddSingleton<VariableConverter>()
                .AddSingleton<TypeConverter>()
                .AddSingleton<ITypeMap, TypeMap>()
                .TryAddProvider<ITokenFormatter, JsonTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, YamlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<ITokenFormatter, XmlTokenFormatter>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, LiteralHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, CodeHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IExpressionHandler, VariableHandler>(ServiceLifetime.Singleton)
                .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
                .AddTransient<IWorkflowRegistry, WorkflowRegistry>()
                .AddScoped<IWorkflowScheduler, WorkflowScheduler>()
                .AddSingleton<IWorkflowSchedulerQueue, WorkflowSchedulerQueue>()
                .AddScoped<IWorkflowHost, WorkflowHost>()
                .AddSingleton<IWorkflowActivator, WorkflowActivator>()
                .AddSingleton<MemoryWorkflowDefinitionVersionStore>()
                .AddSingleton<MemoryWorkflowInstanceStore>()
                .AddStartupRunner()
                .AddTransient<IActivityResolver, ActivityResolver>()
                .AddTransient<IWorkflowProvider, StoreWorkflowProvider>()
                .AddTransient<IWorkflowProvider, CodeWorkflowProvider>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddStartupTask<StartServiceBusTask>()
                .AddConsumer<RunWorkflow, RunWorkflowHandler>()
                .AddAutoMapperProfile<WorkflowDefinitionVersionProfile>(ServiceLifetime.Singleton)
                .AddSerializationHandlers()
                .AddMetadataHandlers()
                .AddPrimitiveActivities();

            return configuration;
        }

        private static IServiceCollection AddMetadataHandlers(this IServiceCollection services) =>
            services
                .AddSingleton<IActivityPropertyOptionsProvider, SelectOptionsProvider>()
                .AddSingleton<IActivityPropertyOptionsProvider, WorkflowExpressionOptionsProvider>();

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
                .AddTypeAlias<object>()
                .AddTypeAlias<bool>()
                .AddTypeAlias<int>()
                .AddTypeAlias<long>()
                .AddTypeAlias<decimal>()
                .AddTypeAlias<string>()
                .AddTypeAlias<IActivity>("Activity")
                .AddTypeAlias(typeof(IList<>), "List")
                .AddTypeAlias(typeof(ICollection<>), "Collection")
                .AddTypeAlias<PathString>()
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

        private static ElsaOptions AddServiceBus(this ElsaOptions options)
        {
            options.WithServiceBus(options.ServiceBusConfigurer);
            return options;
        }
    }
}