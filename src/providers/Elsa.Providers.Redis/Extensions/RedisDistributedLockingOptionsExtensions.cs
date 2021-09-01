using System;
using Elsa.Options;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Elsa.Extensions
{
    public static class RedisDistributedLockingOptionsExtensions
    {
        public static DistributedLockingOptionsBuilder UseRedisLockProvider(this DistributedLockingOptionsBuilder options)
        {
            options.Services.AddRedLockFactory();
            options.UseProviderFactory(CreateRedisDistributedLockFactory);

            return options;
        }

        private static Func<string, IDistributedLock> CreateRedisDistributedLockFactory(IServiceProvider services)
        {
            var multiplexer = services.GetRequiredService<IConnectionMultiplexer>();
            return name => new RedisDistributedLock(name, multiplexer.GetDatabase());
        }

        private static IServiceCollection AddRedLockFactory(this IServiceCollection services) =>
            services.AddSingleton<IDistributedLockFactory, RedLockFactory>(sp => RedLockFactory.Create(
                new[]
                {
                    new RedLockMultiplexer(sp.GetRequiredService<IConnectionMultiplexer>())
                }));
    }
}