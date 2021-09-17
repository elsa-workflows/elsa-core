using System;
using Elsa.Activities.Webhooks.Options;
using Elsa.Caching;
using Elsa.Webhooks.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks
{
    public class WebhookOptionsBuilder
    {
        public WebhookOptionsBuilder(IServiceCollection services) : this(services, new WebhookOptions())
        {
        }

        public WebhookOptionsBuilder(IServiceCollection services, WebhookOptions webhookOptions)
        {
            Services = services;
            WebhookOptions = webhookOptions;
        }

        public WebhookOptions WebhookOptions { get; }
        public IServiceCollection Services { get; }

        public WebhookOptionsBuilder UseWebhookDefinitionStore(Func<IServiceProvider, IWebhookDefinitionStore> factory)
        {
            WebhookOptions.WebhookDefinitionStoreFactory = factory;
            return this;
        }

        public void ApplyTo(WebhookOptions webhookOptions)
        {
            webhookOptions.WebhookDefinitionStoreFactory = WebhookOptions.WebhookDefinitionStoreFactory;
        }
    }
}