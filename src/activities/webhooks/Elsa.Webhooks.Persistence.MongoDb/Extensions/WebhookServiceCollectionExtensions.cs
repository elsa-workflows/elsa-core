using System;
using Elsa.Activities.Webhooks;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Webhooks.Persistence.MongoDb.Services;
using Elsa.Runtime;
using Elsa.Webhooks.Persistence.MongoDb.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Persistence.MongoDb.Extensions
{
    public static class WebhookServiceCollectionExtensions
    {
        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence(this WebhookOptionsBuilder webhookOptions) => UseWebhookMongoDbPersistence<ElsaMongoDbContext>(webhookOptions);

        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence<TDbContext>(this WebhookOptionsBuilder webhookOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(webhookOptions);

            return webhookOptions;
        }

        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence(this WebhookOptionsBuilder webhookOptions, Action<ElsaMongoDbOptions> configureOptions) => UseWebhookMongoDbPersistence<ElsaMongoDbContext>(webhookOptions, configureOptions);

        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence<TDbContext>(this WebhookOptionsBuilder webhookOptions, Action<ElsaMongoDbOptions> configureOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(webhookOptions);
            webhookOptions.Services.Configure(configureOptions);

            return webhookOptions;
        }

        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence(this WebhookOptionsBuilder webhookOptions, IConfiguration configuration) => UseWebhookMongoDbPersistence<ElsaMongoDbContext>(webhookOptions, configuration);

        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence<TDbContext>(this WebhookOptionsBuilder webhookOptions, IConfiguration configuration) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(webhookOptions);
            webhookOptions.Services.Configure<ElsaMongoDbOptions>(configuration);
            return webhookOptions;
        }

        private static void AddCore<TDbContext>(WebhookOptionsBuilder webhookOptions) where TDbContext : ElsaMongoDbContext
        {
            webhookOptions.Services
                .AddScoped<MongoDbWebhookDefinitionStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<ElsaMongoDbContext, TDbContext>()
                .AddScoped<ElsaMongoDbContextProvider>()
                .AddScoped<Func<IMongoCollection<WebhookDefinition>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContextProvider>().WebhookDefinitions)
                .AddStartupTask<DatabaseInitializer>();

            webhookOptions.UseWebhookDefinitionStore(sp => sp.GetRequiredService<MongoDbWebhookDefinitionStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
