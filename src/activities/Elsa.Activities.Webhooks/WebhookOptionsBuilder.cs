using System;
using System.Linq;
using System.Reflection;
using Elsa.Activities.Webhooks.Options;
using Elsa.Caching;
using Elsa.Webhooks.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Rebus.DataBus.InMem;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;

namespace Elsa.Activities.Webhooks
{
    public class WebhookOptionsBuilder
    {
        public WebhookOptionsBuilder(IServiceCollection services)
        {
            WebhookOptions = new WebhookOptions();
            Services = services;

            AddAutoMapper = () =>
            {
                // The profiles are added to AddWorkflowsCore so that they are not forgotten in case the AddAutoMapper function(option) is overridden.
                services.AddAutoMapper(Enumerable.Empty<Assembly>(), ServiceLifetime.Singleton);
            };

            services.AddSingleton<InMemNetwork>();
            services.AddSingleton<InMemorySubscriberStore>();
            services.AddSingleton<InMemDataStore>();
            services.AddMemoryCache();
            services.AddSingleton<ICacheSignal, CacheSignal>();

            DistributedLockingOptionsBuilder = new DistributedLockingOptionsBuilder(this);
        }

        public WebhookOptions WebhookOptions { get; }
        public IServiceCollection Services { get; }
        public DistributedLockingOptionsBuilder DistributedLockingOptionsBuilder { get; }
        internal Action AddAutoMapper { get; private set; }

        public WebhookOptionsBuilder UseWebhookDefinitionStore(Func<IServiceProvider, IWebhookDefinitionStore> factory)
        {
            WebhookOptions.WebhookDefinitionStoreFactory = factory;
            return this;
        }
    }
}
