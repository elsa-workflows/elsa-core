using System;
using System.Linq;
using Elsa.Data.Services;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Data.Extensions
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services,
            Action<IServiceProvider, IConfiguration> configure)
        {
            services
                .AddWorkflowProvider<DatabaseWorkflowProvider>()
                .AddSingleton(sp => CreateStore(sp, configure))
                .AddScoped(CreateSession)
                .AddSingleton<IDataMigrationManager, DataMigrationManager>()
                .AddStartupTask<DatabaseInitializer>()
                .AddStartupTask<DataMigrationsRunner>();

            return services;
        }

        private static IStore CreateStore(
            IServiceProvider serviceProvider,
            Action<IServiceProvider, Configuration> configure)
        {
            var configuration = new Configuration();
            configure(serviceProvider, configuration);
            
            // The following line is a temporary workaround until the bug in YesSql is fixed: https://github.com/sebastienros/yessql/pull/280
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