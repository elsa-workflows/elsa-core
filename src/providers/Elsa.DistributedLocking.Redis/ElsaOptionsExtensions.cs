using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Elsa
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseRedisLockProvider(this ElsaOptions options, string connectionString, TimeSpan? lockTimeout = null)
        {
            options.UseStackExchangeConnectionMultiplexer(connectionString)
                .UseRedLockFactory()
                .UseDistributedLockProvider(
                    sp => new RedisLockProvider(
                        sp.GetRequiredService<IDistributedLockFactory>(),
                        lockTimeout ?? TimeSpan.FromMinutes(1),
                        sp.GetRequiredService<ILogger<RedisLockProvider>>()));

            return options;
        }

        private static ElsaOptions UseStackExchangeConnectionMultiplexer(this ElsaOptions options, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            options.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(connectionString));

            return options;
        }

        private static ElsaOptions UseRedLockFactory(this ElsaOptions options)
        {
            options.Services.AddSingleton<IDistributedLockFactory, RedLockFactory>(
                sp => RedLockFactory.Create(
                    new List<RedLockMultiplexer>
                    {
                        new RedLockMultiplexer(sp.GetRequiredService<IConnectionMultiplexer>())
                    }));

            return options;
        }
    }
}