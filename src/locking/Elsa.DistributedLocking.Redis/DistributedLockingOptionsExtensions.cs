using System;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Elsa
{
    public static class DistributedLockingOptionsExtensions
    {
        public static DistributedLockingOptionsBuilder UseRedisLockProvider(this DistributedLockingOptionsBuilder options, string connectionString)
        {
            options
                .Services
                .UseStackExchangeConnectionMultiplexer(connectionString)
                .UseRedLockFactory();

            options.UseProviderFactory(CreateRedisDistributedLockFactory);

            return options;
        }

        private static Func<string, IDistributedLock> CreateRedisDistributedLockFactory(IServiceProvider services)
        {
            var multiplexer = services.GetRequiredService<IConnectionMultiplexer>();
            return name => new RedisDistributedLock(name, multiplexer.GetDatabase());
        }

        private static IServiceCollection UseStackExchangeConnectionMultiplexer(this IServiceCollection services, string connectionString) => services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(connectionString));

        private static IServiceCollection UseRedLockFactory(this IServiceCollection services) =>
            services.AddSingleton<IDistributedLockFactory, RedLockFactory>(sp => RedLockFactory.Create(
                new[]
                {
                    new RedLockMultiplexer(sp.GetRequiredService<IConnectionMultiplexer>())
                }));
    }
}