using Elsa.DistributedLock;
using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.DistributedLocking.Redis
{
    public class RedisLockProvider : DistributedLockProvider
    {
        private const string Prefix = "elsa";
        private readonly ILogger logger;
        private readonly string connectionString;
        private readonly TimeSpan lockTimeout;
        private IConnectionMultiplexer multiplexer;
        private RedLockFactory lockFactory;
        private readonly List<IRedLock> RedLockInstance = new List<IRedLock>();

        public RedisLockProvider(string connectionString, TimeSpan lockTimeout, ILogger<RedisLockProvider> logger)
        {
            this.logger = logger;
            this.connectionString = connectionString;
            this.lockTimeout = lockTimeout;
        }

        public override async Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
        {
            if (lockFactory == null)
                throw new InvalidOperationException();

            var redLock = await lockFactory.CreateLockAsync($"{Prefix}:{name}", lockTimeout,
                                                                        TimeSpan.FromSeconds(1),
                                                                        TimeSpan.FromMilliseconds(10),
                                                                        cancellationToken);

            if (redLock.IsAcquired)
            {
                lock (RedLockInstance)
                {
                    RedLockInstance.Add(redLock);
                }
                return true;
            }

            return false;
        }

        public override Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default)
        {
            if (lockFactory == null)
                throw new InvalidOperationException();

            lock (RedLockInstance)
            {
                foreach (var redLock in RedLockInstance)
                {
                    if (redLock.Resource == $"{Prefix}:{name}")
                    {
                        redLock.Dispose();
                        RedLockInstance.Remove(redLock);
                        break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public override async Task<bool> SetupAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                multiplexer = await ConnectionMultiplexer.ConnectAsync(this.connectionString);
                lockFactory = RedLockFactory.Create(new List<RedLockMultiplexer>() { new RedLockMultiplexer(multiplexer) });
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Unable to setup redis lock provider {ex}");
                throw ex;
            }
        }

        public override async Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                lockFactory?.Dispose();
                await multiplexer.CloseAsync();
                multiplexer = null;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Unable to dispose redis lock provider {ex}");
            }
        }
    }
}