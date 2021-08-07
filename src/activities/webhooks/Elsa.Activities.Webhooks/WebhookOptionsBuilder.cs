using System;
using Elsa.Activities.Webhooks.Options;
using Elsa.Caching;
using Elsa.Webhooks.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks
{
    public class WebhookOptionsBuilder
    {
        public WebhookOptionsBuilder(IServiceCollection services)
        {
            WebhookOptions = new WebhookOptions();
            Services = services;
            services.AddMemoryCache();
            services.AddSingleton<ICacheSignal, CacheSignal>();
        }

        public WebhookOptions WebhookOptions { get; }
        public IServiceCollection Services { get; }

        public WebhookOptionsBuilder UseWebhookDefinitionStore(Func<IServiceProvider, IWebhookDefinitionStore> factory)
        {
            WebhookOptions.WebhookDefinitionStoreFactory = factory;
            return this;
        }
    }
}
