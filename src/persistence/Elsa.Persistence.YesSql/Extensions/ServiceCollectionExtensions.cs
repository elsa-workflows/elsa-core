using System;
using System.Data;
using System.Linq;
using Elsa.Data;
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

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseYesSqlPersistence(this ElsaOptions elsa) => elsa.UseYesSqlPersistence(config => config.UseSqLite("Data Source=elsa.db;Cache=Shared", IsolationLevel.ReadUncommitted));
        public static ElsaOptions UseYesSqlPersistence(this ElsaOptions elsa, Action<IConfiguration> configure) => elsa.UseYesSqlPersistence((_, config) => configure(config));

        public static ElsaOptions UseYesSqlPersistence(this ElsaOptions elsa, Action<IServiceProvider, IConfiguration> configure)
        {
            elsa.Services
                .AddScoped<YesSqlWorkflowDefinitionStore>()
                .AddScoped<YesSqlWorkflowInstanceStore>()
                .AddScoped<YesSqlWorkflowExecutionLogStore>()
                .AddSingleton(sp => CreateStore(sp, configure))
                .AddScoped(CreateSession)
                .AddScoped<IDataMigrationManager, DataMigrationManager>()
                .AddStartupTask<DatabaseInitializer>()
                .AddStartupTask<DataMigrationsRunner>()
                .AddDataMigration<Migrations>()
                .AddAutoMapperProfile<AutoMapperProfile>()
                .AddIndexProvider<WorkflowDefinitionIndexProvider>()
                .AddIndexProvider<WorkflowInstanceIndexProvider>()
                .AddIndexProvider<WorkflowExecutionLogRecordIndexProvider>();

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<YesSqlWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<YesSqlWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<YesSqlWorkflowExecutionLogStore>());
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
            var store = serviceProvider.GetRequiredService<IStore>();
            var session = store.CreateSession();
            var scopedServices = serviceProvider.GetServices<IScopedIndexProvider>().Cast<IIndexProvider>().ToArray();

            session.RegisterIndexes(scopedServices);

            return session;
        }
    }
}