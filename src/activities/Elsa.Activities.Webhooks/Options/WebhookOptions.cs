using System;
using Elsa.Activities.Webhooks.Persistence;
using Elsa.Activities.Webhooks.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks.Options
{
    public class WebhookOptions
    {
        public WebhookOptions()
        {
            WebhookDefinitionStoreFactory = provider => ActivatorUtilities.CreateInstance<InMemoryWebhookStore>(provider);
        }

        internal Func<IServiceProvider, IWebhookDefinitionStore> WebhookDefinitionStoreFactory { get; set; }

        public WebhookOptions UseWebhookDefinitionStore(Func<IServiceProvider, IWebhookDefinitionStore> factory)
        {
            WebhookDefinitionStoreFactory = factory;
            return this;
        }
    }
}
