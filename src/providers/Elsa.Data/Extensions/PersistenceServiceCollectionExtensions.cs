using System;
using System.Linq;
using Elsa.Data.Services;
using Elsa.Persistence;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Data.Extensions
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static ElsaOptions UsePersistence(
            this ElsaOptions options,
            Action<IServiceProvider, Configuration> configure)
        {
            var services = options.Services;

            services
                .AddWorkflowProvider<DatabaseWorkflowProvider>()
                .AddSingleton(sp => CreateStore(sp, configure))
                .AddScoped(CreateSession)
                .AddStartupTask<DatabaseInitializer>()
                .AddStartupTask<DataMigrationsRunner>();

            return options;
        }

        public static ElsaOptions UsePersistence(
            this ElsaOptions options,
            Action<Configuration> configure) =>
            options.UsePersistence((sp, config) => configure(config));

        private static IStore CreateStore(
            IServiceProvider serviceProvider,
            Action<IServiceProvider, Configuration> configure)
        {
            var configuration = new Configuration();
            configure(serviceProvider, configuration);

            var store = StoreFactory.Create(configuration);
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