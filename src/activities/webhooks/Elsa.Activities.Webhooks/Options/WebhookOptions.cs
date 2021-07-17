using System;
using Elsa.Activities.Webhooks.Persistence.InMemory;
using Elsa.Webhooks.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks.Options
{
    public class WebhookOptions
    {
        public WebhookOptions()
        {
            WebhookDefinitionStoreFactory = provider => ActivatorUtilities.CreateInstance<InMemoryWebhookDefinitionStore>(provider);
        }

        public Func<IServiceProvider, IWebhookDefinitionStore> WebhookDefinitionStoreFactory { get; set; }
    }
}
