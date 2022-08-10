using Elsa.Caching;
using Elsa.Options;
using Elsa.Runtime;
using Elsa.Services;
using Elsa.StartupTasks;

namespace Elsa.Extensions
{
    public static class RedisCacheSignalElsaOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder UseRedisCacheSignal(this ElsaOptionsBuilder builder)
        {
            var services = builder.ContainerBuilder;
            
            services
                .AddMultiton<RedisBus>()
                .AddStartupTask<SubscribeToRedisCacheSignals>()
                .Decorate<ICacheSignal, RedisCacheSignal>();

            return builder;
        }
    }
}