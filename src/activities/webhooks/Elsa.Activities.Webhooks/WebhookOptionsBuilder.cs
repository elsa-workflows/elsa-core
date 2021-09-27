using System;
using Elsa.Activities.Webhooks.Options;
using Elsa.Webhooks.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks
{
    public class WebhookOptionsBuilder
    {
        public WebhookOptionsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public WebhookOptions WebhookOptions { get; } = new();
        public IServiceCollection Services { get; }

        public WebhookOptionsBuilder UseWebhookDefinitionStore(Func<IServiceProvider, IWebhookDefinitionStore> factory)
        {
            WebhookOptions.WebhookDefinitionStoreFactory = factory;
            return this;
        }
    }
}