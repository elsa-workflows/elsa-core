using System;
using System.Data;
using Elsa.Options;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Mapping;
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
            elsa.Services
                .AddScoped<YesSqlWorkflowDefinitionStore>()
                .AddScoped<YesSqlWorkflowInstanceStore>()
                .AddScoped<YesSqlWorkflowExecutionLogStore>()
                .AddScoped<YesSqlBookmarkStore>()
                .AddScoped<YesSqlTriggerStore>()
                .AddSingleton(sp => CreateStore(sp, configure))
                .AddSingleton<ISessionProvider, SessionProvider>()
                .AddScoped(CreateSession)
                .AddScoped<IDataMigrationManager, DataMigrationManager>()
                .AddStartupTask<DatabaseInitializer>()
                .AddStartupTask<RunMigrations>()
                .AddDataMigration<Migrations>()
                .AddAutoMapperProfile<AutoMapperProfile>()
                .AddIndexProvider<WorkflowDefinitionIndexProvider>()
                .AddIndexProvider<WorkflowInstanceIndexProvider>()
                .AddIndexProvider<WorkflowExecutionLogRecordIndexProvider>()
                .AddIndexProvider<BookmarkIndexProvider>()
                .AddIndexProvider<TriggerIndexProvider>();

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<YesSqlWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<YesSqlWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<YesSqlWorkflowExecutionLogStore>())
                .UseBookmarkStore(sp => sp.GetRequiredService<YesSqlBookmarkStore>())
                .UseTriggerStore(sp => sp.GetRequiredService<YesSqlTriggerStore>());
        }

        public static IServiceCollection AddIndexProvider<T>(this IServiceCollection services) where T : class, IIndexProvider => services.AddSingleton<IIndexProvider, T>();
        public static IServiceCollection AddScopedIndexProvider<T>(this IServiceCollection services) where T : class, IIndexProvider => services.AddScoped<IScopedIndexProvider>();
        public static IServiceCollection AddDataMigration<T>(this IServiceCollection services) where T : class, IDataMigration => services.AddScoped<IDataMigration, T>();

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