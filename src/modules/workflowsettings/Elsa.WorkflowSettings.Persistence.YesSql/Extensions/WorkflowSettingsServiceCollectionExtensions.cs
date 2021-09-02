using System;
using System.Data;
using Elsa.Persistence.YesSql;
using Elsa.Persistence.YesSql.Data;
using Elsa.Runtime;
using Elsa.WorkflowSettings.Persistence.YesSql.Indexes;
using Elsa.WorkflowSettings.Persistence.YesSql.Mapping;
using Elsa.WorkflowSettings.Persistence.YesSql.Services;
using Elsa.WorkflowSettings.Persistence.YesSql.Stores;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;
using YesSql.Provider.Sqlite;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Extensions
{
    public static class WorkflowSettingsServiceCollectionExtensions
    {
        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsYesSqlPersistence(this WorkflowSettingsOptionsBuilder workflowSettingsOptions) => workflowSettingsOptions.UseWorkflowSettingsYesSqlPersistence(config => config.UseSqLite("Data Source=elsa.yessql.db;Cache=Shared", IsolationLevel.ReadUncommitted));
        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsYesSqlPersistence(this WorkflowSettingsOptionsBuilder workflowSettingsOptions, Action<IConfiguration> configure) => workflowSettingsOptions.UseWorkflowSettingsYesSqlPersistence((_, config) => configure(config));

        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsYesSqlPersistence(this WorkflowSettingsOptionsBuilder workflowSettingsOptions, Action<IServiceProvider, IConfiguration> configure)
        {
            workflowSettingsOptions.Services
                .AddScoped<YesSqlWorkflowSettingsStore>()
                .AddSingleton(sp => CreateStore(sp, configure))
                .AddStartupTask<DatabaseInitializer>()
                .AddDataMigration<Migrations>()
                .AddAutoMapperProfile<AutoMapperProfile>()
                .AddIndexProvider<WorkflowSettingsIndexProvider>();

            workflowSettingsOptions.UseWorkflowSettingsStore(sp => sp.GetRequiredService<YesSqlWorkflowSettingsStore>());

            return workflowSettingsOptions;
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
            var provider = serviceProvider.GetRequiredService<Elsa.Persistence.YesSql.Services.ISessionProvider>();
            return provider.CreateSession();
        }
    }
}
