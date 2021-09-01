using Elsa.Caching.Rebus.Consumers;
using Elsa.Caching.Rebus.Messages;
using Elsa.Caching.Rebus.Services;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Caching.Rebus.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseRebusCacheSignal(this ElsaOptionsBuilder builder)
        {
            var services = builder.Services;
            services.Decorate<ICacheSignal, RebusCacheSignal>();
            builder.AddPubSubConsumer<TriggerCacheSignalConsumer, TriggerCacheSignal>();

            return builder;
        }
    }
}