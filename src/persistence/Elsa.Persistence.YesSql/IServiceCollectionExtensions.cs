using System;
using System.Linq;

using Elsa.Data;
using Elsa.Data.Services;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Services;
using Elsa.Repositories;
using Elsa.Runtime;
using Elsa.WorkflowProviders;

using Microsoft.Extensions.DependencyInjection;

using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaPersistenceYesSql(this IServiceCollection services, Action<IServiceProvider, IConfiguration> configure)
        {
            // Scoped, because IMediator is registered as Scoped and IIdGenerator could also be registered as Scoped
            services.AddScoped<IWorkflowDefinitionRepository, YesSqlWorkflowDefinitionRepository>()
                .AddScoped<IWorkflowInstanceRepository, YesSqlWorkflowInstanceRepository>()
               .AddWorkflowProvider<DatabaseWorkflowProvider>()
               .AddSingleton(sp => CreateStore(sp, configure))
               .AddScoped(CreateSession)
               .AddScoped<IDataMigrationManager, DataMigrationManager>()
               .AddStartupTask<DatabaseInitializer>()
               .AddStartupTask<DataMigrationsRunner>()
               .AddDataMigration<Migrations>()
               .AddIndexProvider<WorkflowDefinitionIndexProvider>()
               .AddIndexProvider<WorkflowInstanceIndexProvider>();

            return services;
        }

        public static IServiceCollection AddIndexProvider<T>(this IServiceCollection services) where T : class, IIndexProvider => services.AddSingleton<IIndexProvider, T>();
        public static IServiceCollection AddScopedIndexProvider<T>(this IServiceCollection services) where T : class, IIndexProvider => services.AddScoped<IScopedIndexProvider>();
        public static IServiceCollection AddDataMigration<T>(this IServiceCollection services) where T : class, IDataMigration => services.AddScoped<IDataMigration, T>();

        private static IStore CreateStore(
            IServiceProvider serviceProvider,
            Action<IServiceProvider, Configuration> configure)
        {
            var configuration = new Configuration { ContentSerializer = new CustomJsonContentSerializer() };
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
