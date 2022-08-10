using Elsa.Activities.Webhooks;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.Attributes;
using Elsa.Extensions;
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

            var webhookOptionsBuilder = new WebhookOptionsBuilder(services, elsa.ContainerBuilder);
            webhookOptionsBuilder.UseWebhookMongoDbPersistence();

            elsa.ContainerBuilder.AddScoped(sp => webhookOptionsBuilder.WebhookOptions.WebhookDefinitionStoreFactory(sp));
            elsa.ContainerBuilder.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();
            elsa.ContainerBuilder.Decorate<IWebhookDefinitionStore, EventPublishingWebhookDefinitionStore>();
        }
    }
}
