using System;
using System.Data;
using Autofac;
using Autofac.Multitenant;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
using Elsa.Options;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Mapping;
using Elsa.Persistence.YesSql.Options;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.Stores;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;
using YesSql.Provider.Sqlite;

namespace Elsa.Persistence.YesSql
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseYesSqlPersistence(this ElsaOptionsBuilder elsa) => elsa.UseYesSqlPersistence(config => config.UseSqLite("Data Source=elsa.yessql.db;Cache=Shared", IsolationLevel.ReadUncommitted));
        public static ElsaOptionsBuilder UseYesSqlPersistence(this ElsaOptionsBuilder elsa, Action<IConfiguration> configure) => elsa.UseYesSqlPersistence((_, config) => configure(config));

        public static ElsaOptionsBuilder UseYesSqlPersistence(this ElsaOptionsBuilder elsa, Action<IServiceProvider, IConfiguration> configure)
        {
            if (elsa.ContainerBuilder == null)
                throw new ArgumentNullException("Cannot setup Entity Framework persistence for multitenancy when ContainerBuilder is null");

            elsa.ContainerBuilder
                .Register(cc =>
                {
                    var tenant = cc.Resolve<ITenant>();
                    return new ElsaDbOptions(tenant!.GetDatabaseConnectionString()!);
                }).InstancePerTenant();

            elsa.ContainerBuilder
                .AddScoped<YesSqlWorkflowDefinitionStore>()
                .AddScoped<YesSqlWorkflowInstanceStore>()
                .AddScoped<YesSqlWorkflowExecutionLogStore>()
                .AddScoped<YesSqlBookmarkStore>()
                .AddScoped<YesSqlTriggerStore>()
                .AddMultiton(sp => CreateStore(sp, configure))
                .AddMultiton<ISessionProvider, SessionProvider>()
                .AddScoped(CreateSession)
                .AddScoped<IDataMigrationManager, DataMigrationManager>()
                .AddStartupTask<DatabaseInitializer>()
                .AddStartupTask<RunMigrations>()
                .AddDataMigration<Migrations>()
                .AddIndexProvider<WorkflowDefinitionIndexProvider>()
                .AddIndexProvider<WorkflowInstanceIndexProvider>()
                .AddIndexProvider<WorkflowExecutionLogRecordIndexProvider>()
                .AddIndexProvider<BookmarkIndexProvider>()
                .AddIndexProvider<TriggerIndexProvider>();

            elsa.Services
                .AddAutoMapperProfile<AutoMapperProfile>();

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<YesSqlWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<YesSqlWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<YesSqlWorkflowExecutionLogStore>())
                .UseBookmarkStore(sp => sp.GetRequiredService<YesSqlBookmarkStore>())
                .UseTriggerStore(sp => sp.GetRequiredService<YesSqlTriggerStore>());
        }

        public static ContainerBuilder AddIndexProvider<T>(this ContainerBuilder containerBuilder) where T : class, IIndexProvider => containerBuilder.AddMultiton<IIndexProvider, T>();
        public static ContainerBuilder AddScopedIndexProvider<T>(this ContainerBuilder containerBuilder) where T : class, IIndexProvider => containerBuilder.AddScoped<IScopedIndexProvider>();
        public static ContainerBuilder AddDataMigration<T>(this ContainerBuilder containerBuilder) where T : class, IDataMigration => containerBuilder.AddScoped<IDataMigration, T>();

        private static IStore CreateStore(
            IServiceProvider serviceProvider,
            Action<IServiceProvider, Configuration> configure)
        {
            var configuration = new Configuration
            {
                ContentSerializer = new CustomJsonContentSerializer()
            };
            
            configure(serviceProvider, configuration);

            // TODO: The following line is a temporary workaround until the bug in YesSql is fixed: https://github.com/sebastienros/yessql/pull/280
            var store = StoreFactory.CreateAndInitializeAsync(configuration).GetAwaiter().GetResult();
            //var store = StoreFactory.Create(configuration);

            var indexes = serviceProvider.GetServices<IIndexProvider>();
            store.RegisterIndexes(indexes);

            return store;
        }

        private static ISession CreateSession(IServiceProvider serviceProvider)
        {
            var provider = serviceProvider.GetRequiredService<ISessionProvider>();
            return provider.CreateSession();
        }
    }
}