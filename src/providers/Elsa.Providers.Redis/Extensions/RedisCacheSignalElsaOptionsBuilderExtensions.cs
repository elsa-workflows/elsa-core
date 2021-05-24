using Elsa.Caching;
using Elsa.Runtime;
using Elsa.Services;
using Elsa.StartupTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class RedisCacheSignalElsaOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder UseRedisCacheSignal(this ElsaOptionsBuilder builder)
        {
            var services = builder.Services;
            
            services
                .AddSingleton<RedisBus>()
                .AddStartupTask<SubscribeToRedisCacheSignals>()
                .Decorate<ICacheSignal, RedisCacheSignal>();

            return builder;
        }
    }
}