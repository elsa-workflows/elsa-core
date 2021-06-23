using System;
using Elsa.Activities.Webhooks;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Webhooks.Persistence.MongoDb.Services;
using Elsa.Runtime;
using Elsa.Webhooks.Persistence.MongoDb.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.MongoDb.Extensions
{
    public static class WebhookServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa, Action<ElsaMongoDbOptions> configureOptions) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa, configureOptions);

        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa, Action<ElsaMongoDbOptions> configureOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(elsa);
            elsa.Services.Configure(configureOptions);

            return elsa;
        }

        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa, IConfiguration configuration) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa, configuration);

        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa, IConfiguration configuration) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(elsa);
            elsa.Services.Configure<ElsaMongoDbOptions>(configuration);
            return elsa;
        }

        private static void AddCore<TDbContext>(ElsaOptionsBuilder elsa) where TDbContext : ElsaMongoDbContext
        {
            elsa.Services
                .AddSingleton<MongoDbWebhookDefinitionStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<ElsaMongoDbContext, TDbContext>()
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().WebhookDefinitions)
                .AddStartupTask<DatabaseInitializer>();

            var webhookOptionsBuilder = new WebhookOptionsBuilder(elsa.Services);

            webhookOptionsBuilder.UseWebhookDefinitionStore(sp => sp.GetRequiredService<MongoDbWebhookDefinitionStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
