using Autofac;
using Autofac.Multitenant;
using Elsa.Activities.Webhooks;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Runtime;
using Elsa.Webhooks.Persistence.MongoDb.Services;
using Elsa.Webhooks.Persistence.MongoDb.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence(this WebhookOptionsBuilder webhookOptions, IConfiguration configuration) => UseWebhookMongoDbPersistence<ElsaMongoDbContext>(webhookOptions, configuration);

        public static WebhookOptionsBuilder UseWebhookMongoDbPersistence<TDbContext>(this WebhookOptionsBuilder webhookOptions, IConfiguration configuration) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(webhookOptions);
            webhookOptions.Services.Configure<ElsaMongoDbOptions>(configuration);
            return webhookOptions;
        }

        private static void AddCore<TDbContext>(WebhookOptionsBuilder webhookOptions) where TDbContext : ElsaMongoDbContext
        {
            webhookOptions.ContainerBuilder
              .Register(cc =>
              {
                  var tenant = cc.Resolve<ITenant>();
                  return new ElsaMongoDbOptions() { ConnectionString = tenant!.GetDatabaseConnectionString()! };
              }).IfNotRegistered(typeof(ElsaMongoDbOptions)).InstancePerTenant();

            webhookOptions.ContainerBuilder
                .AddMultiton<MongoDbWebhookDefinitionStore>()
                .AddMultiton<TDbContext>()
                .AddMultiton<ElsaMongoDbContext, TDbContext>()
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().WebhookDefinitions)
                .AddStartupTask<DatabaseInitializer>();

            webhookOptions.UseWebhookDefinitionStore(sp => sp.GetRequiredService<MongoDbWebhookDefinitionStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
