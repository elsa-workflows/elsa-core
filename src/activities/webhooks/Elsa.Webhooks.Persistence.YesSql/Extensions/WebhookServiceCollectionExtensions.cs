using System;
using System.Data;
using Elsa.Activities.Webhooks;
using Elsa.Webhooks.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql;
using Elsa.Runtime;
using Elsa.Webhooks.Persistence.YesSql.Indexes;
using Elsa.Webhooks.Persistence.YesSql.Stores;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;
using YesSql.Provider.Sqlite;
using Elsa.Persistence.YesSql.Data;
using Elsa.Webhooks.Persistence.YesSql.Mapping;

namespace Elsa.Webhooks.Persistence.YesSql.Extensions
{
    public static class WebhookServiceCollectionExtensions
    {
        public static WebhookOptionsBuilder UseWebhookYesSqlPersistence(this WebhookOptionsBuilder webhookOptions) => webhookOptions.UseWebhookYesSqlPersistence(config => config.UseSqLite("Data Source=elsa.yessql.db;Cache=Shared", IsolationLevel.ReadUncommitted));
        public static WebhookOptionsBuilder UseWebhookYesSqlPersistence(this WebhookOptionsBuilder webhookOptions, Action<IConfiguration> configure) => webhookOptions.UseWebhookYesSqlPersistence((_, config) => configure(config));

        public static WebhookOptionsBuilder UseWebhookYesSqlPersistence(this WebhookOptionsBuilder webhookOptions, Action<IServiceProvider, IConfiguration> configure)
        {
            webhookOptions.Services
                .AddScoped<YesSqlWebhookDefinitionStore>()
                .AddSingleton(sp => CreateStore(sp, configure))
                .AddStartupTask<DatabaseInitializer>()
                .AddDataMigration<Migrations>()
                .AddAutoMapperProfile<AutoMapperProfile>()
                .AddIndexProvider<WebhookDefinitionIndexProvider>();

            webhookOptions.UseWebhookDefinitionStore(sp => sp.GetRequiredService<YesSqlWebhookDefinitionStore>());

            return webhookOptions;
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
