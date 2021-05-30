using System;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks
{
    public class DistributedLockingOptionsBuilder
    {
        public DistributedLockingOptionsBuilder(WebhookOptionsBuilder optionsBuilder) => WebhookOptionsBuilder = optionsBuilder;
        public WebhookOptionsBuilder WebhookOptionsBuilder { get; }
        public IServiceCollection Services => WebhookOptionsBuilder.Services;

        public DistributedLockingOptionsBuilder UseProviderFactory(Func<IServiceProvider, Func<string, IDistributedLock>> factory)
        {
            WebhookOptionsBuilder.WebhookOptions.DistributedLockingOptions.DistributedLockProviderFactory = factory;
            return this;
        }
    }
}