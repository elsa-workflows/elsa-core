using System;
using System.Collections.Generic;
using Elsa;
using Elsa.Activities;
using Elsa.AutoMapper.Extensions;
using Elsa.Caching;
using Elsa.Converters;
using Elsa.Expressions;
using Elsa.Mapping;
using Elsa.Messages.Handlers;
using Elsa.Persistence;
using Elsa.Persistence.Memory;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Serialization.Handlers;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.WorkflowBuilders;
using Elsa.WorkflowProviders;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaCore(
            this IServiceCollection services,
            Action<ElsaBuilder> configure = null)
        {
            var configuration = new ElsaBuilder(services);
            configuration.AddWorkflowsCore();
            configuration.AddMediatR();
            configure?.Invoke(configuration);
            EnsurePersistence(configuration);
            EnsureCaching(configuration);

            return services;
        }

        public static IServiceCollection AddWorkflow<T>(this IServiceCollection services)
            where T : class, IWorkflow =>
            services.AddTransient<IWorkflow, T>();

        public static IServiceCollection AddActivity<T>(this IServiceCollection services)
            where T : class, IActivity
        {
            return services
                .AddTransient<T>()
                .AddTransient<IActivity>(sp => sp.GetRequiredService<T>());
        }

        public static IServiceCollection AddTypeNameValueHandler<T>(this IServiceCollection services) where T : class, IValueHandler => services.AddTransient<IValueHandler, T>();
        public static IServiceCollection AddTypeAlias<T>(this IServiceCollection services, string alias) => services.AddTypeAlias(typeof(T), alias);
        public static IServiceCollection AddTypeAlias(this IServiceCollection services, Type type, string alias) => services.AddTransient<ITypeAlias>(sp => new TypeAlias(type, alias));

        private static IServiceCollection AddMediatR(this ElsaBuilder configuration)
        {
            return configuration.Services.AddMediatR(
                mediatr => mediatr.AsSingleton(),
                typeof(ElsaServiceCollectionExtensions));
        }

        private static ElsaBuilder AddWorkflowsCore(this ElsaBuilder configuration)
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
                .TryAddProvider<IWorkflowExpressionHandler, LiteralHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IWorkflowExpressionHandler, CodeHandler>(ServiceLifetime.Singleton)
                .TryAddProvider<IWorkflowExpressionHandler, VariableHandler>(ServiceLifetime.Singleton)
                .AddTransient<IWorkflowFactory, WorkflowFactory>()
                .AddScoped<IActivityInvoker, ActivityInvoker>()
                .AddScoped<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>()
                .AddSingleton<IWorkflowSerializerProvider, WorkflowSerializerProvider>()
                .AddTransient<IWorkflowRegistry, WorkflowRegistry>()
                .AddScoped<IWorkflowRunner, WorkflowRunner>()
                .AddScoped<IActivityResolver, ActivityResolver>()
                .AddScoped<IWorkflowEventHandler, ActivityLoggingWorkflowEventHandler>()
                .AddTransient<IWorkflowProvider, StoreWorkflowProvider>()
                .AddTransient<IWorkflowProvider, CodeWorkflowProvider>()
                .AddTransient<IWorkflowBuilder, WorkflowBuilder>()
                .AddTransient<Func<IWorkflowBuilder>>(sp => sp.GetRequiredService<IWorkflowBuilder>)
                .AddAutoMapperProfile<WorkflowDefinitionProfile>(ServiceLifetime.Singleton)
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
                .AddTypeAlias<string>("String")
                .AddTypeAlias<int>("Int32")
                .AddTypeAlias(typeof(IList<>), "List")
                .AddPrimitiveActivities();

            return configuration;
        }

        private static void EnsurePersistence(ElsaBuilder configuration)
        {
            var hasDefinitionStore = configuration.HasService<IWorkflowDefinitionStore>();
            var hasInstanceStore = configuration.HasService<IWorkflowInstanceStore>();

            if (!hasDefinitionStore || !hasInstanceStore)
                configuration.WithMemoryStores();

            configuration.Services.Decorate<IWorkflowDefinitionStore, PublishingWorkflowDefinitionStore>();
        }

        private static void EnsureCaching(ElsaBuilder configuration)
        {
            if (!configuration.HasService<ISignal>())
                configuration.Services.AddSingleton<ISignal, Signal>();

            configuration.Services.AddMemoryCache();
        }

        private static IServiceCollection AddPrimitiveActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<SetVariable>();
        }
    }
}