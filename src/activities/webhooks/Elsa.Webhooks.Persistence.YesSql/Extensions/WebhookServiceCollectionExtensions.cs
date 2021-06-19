using System;
using Elsa.Activities.Webhooks;
using Elsa.Persistence.YesSql;
using Elsa.Webhooks.Persistence.YesSql.Stores;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;
using Elsa.Runtime;
using Elsa.Webhooks.Persistence.YesSql.Mapping;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.Data;
using Elsa.Webhooks.Persistence.YesSql.Indexes;

namespace Elsa.Webhooks.Persistence.YesSql.Extensions
{
    public static class WebhookServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseYesSqlPersistence(this ElsaOptionsBuilder elsa, Action<IServiceProvider, IConfiguration> configure)
        {
            elsa.Services
                .AddScoped<YesSqlWebhookDefinitionStore>()
                .AddSingleton(sp => CreateStore(sp, configure))
                .AddSingleton<ISessionProvider, SessionProvider>()
                .AddScoped(CreateSession)
                .AddScoped<IDataMigrationManager, DataMigrationManager>()
                .AddStartupTask<DatabaseInitializer>()
                .AddStartupTask<RunMigrations>()
                .AddDataMigration<Migrations>()
                .AddAutoMapperProfile<AutoMapperProfile>()
                .AddIndexProvider<WebhookDefinitionIndexProvider>();

            var webhookOptionsBuilder = new WebhookOptionsBuilder(elsa.Services);

            webhookOptionsBuilder.UseWebhookDefinitionStore(sp => sp.GetRequiredService<YesSqlWebhookDefinitionStore>());

            return elsa;
        }

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
