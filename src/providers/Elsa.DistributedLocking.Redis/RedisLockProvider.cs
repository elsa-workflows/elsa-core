using Elsa.DistributedLock;
using Microsoft.Extensions.Logging;
using RedLockNet;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.DistributedLocking.Redis
{
    public class RedisLockProvider : IDistributedLockProvider
    {
        private const string Prefix = "elsa";
        private readonly ILogger _logger;
        private readonly TimeSpan _lockTimeout;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly List<IRedLock> RedLockInstance = new List<IRedLock>();

        public RedisLockProvider(IDistributedLockFactory distributedLockFactory, TimeSpan lockTimeout, ILogger<RedisLockProvider> logger)
        {
            _logger = logger;
            _distributedLockFactory = distributedLockFactory;
            _lockTimeout = lockTimeout;
        }
        public Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateLockAsync(name, cancellationToken);
        }
        private async Task<bool> CreateLockAsync(string name, CancellationToken cancellationToken = default)
        {
            var resourceName = $"{Prefix}:{name}";
            _logger.LogInformation($"Lock provider will try to acquire lock for {resourceName}");
            try
            {
                var redLock = await _distributedLockFactory.CreateLockAsync(resourceName, _lockTimeout,
                                                                           TimeSpan.FromSeconds(1),
                                                                           TimeSpan.FromMilliseconds(10),
                                                                           cancellationToken)
                                                           .ConfigureAwait(false);

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
            catch (Exception ex)
            {
                _logger.LogError($"Failed to acquire lock for {resourceName}. Reason > {ex}");
                throw;
            }
        }

        public Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            var resourceName = $"{Prefix}:{name}";

            _logger.LogInformation($"Lock provider will try to release lock for {resourceName}");
            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to release lock for {resourceName}. Reason > {ex}");
            }
            return Task.CompletedTask;
        }

    }
}