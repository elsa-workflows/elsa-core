using Elsa.Activities.Webhooks;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Elsa.Webhooks.Persistence.MongoDb.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.MongoDb
{
    [Feature("Webhooks:MongoDb")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var services = elsa.Services;
            var section = configuration.GetSection($"Elsa:Features:Webhooks");
            var connectionStringName = section.GetValue<string>("ConnectionStringIdentifier");
            var connectionString = section.GetValue<string>("ConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                if (string.IsNullOrWhiteSpace(connectionStringName))
                    connectionStringName = "MongoDb";

                connectionString = configuration.GetConnectionString(connectionStringName);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "mongodb://localhost:27017/Elsa";

            var webhookOptionsBuilder = new WebhookOptionsBuilder(services);
              webhookOptionsBuilder.UseWebhookMongoDbPersistence(options => options.ConnectionString = connectionString);

            services.AddScoped(sp => webhookOptionsBuilder.WebhookOptions.WebhookDefinitionStoreFactory(sp));
            services.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();
            services.Decorate<IWebhookDefinitionStore, EventPublishingWebhookDefinitionStore>();
        }
    }
}
