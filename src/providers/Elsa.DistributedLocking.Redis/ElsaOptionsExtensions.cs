using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using System;
using System.Collections.Generic;
using System.Net;

namespace Elsa.DistributedLocking.Redis
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseRedisLockProvider(this ElsaOptions options, string connectionString, TimeSpan? lockTimeout = null)
        {
            options
                .UseDistributedLockProvider(sp => new RedisLockProvider(connectionString,
                                                                        lockTimeout.GetValueOrDefault(TimeSpan.FromMinutes(1)),
                                                                        sp.GetRequiredService<ILogger<RedisLockProvider>>()));
            return options;
        }
    }
}