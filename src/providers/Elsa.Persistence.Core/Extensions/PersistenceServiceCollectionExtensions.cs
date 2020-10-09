using System;
using System.Linq;
using Elsa.Persistence.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.Core.Extensions
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static ElsaOptions UsePersistence(
            this ElsaOptions options,
            Action<IServiceProvider, Configuration> configure)
        {
            var services = options.Services;
            services.AddSingleton(sp => CreateStore(sp, configure));
            services.AddScoped(CreateSession);

            return options;
        }

        private static IStore CreateStore(
            IServiceProvider serviceProvider,
            Action<IServiceProvider, Configuration> configure)
        {
            var configuration = new Configuration();
            configure(serviceProvider, configuration);

            // TODO: Instantiate Store manually without using the factory as soon as Sebastien Ros makes the constructor public (see https://github.com/sebastienros/yessql/issues/188)
            var store = StoreFactory.CreateAsync(configuration).GetAwaiter().GetResult();
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