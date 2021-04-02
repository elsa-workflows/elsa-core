using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.DistributedLocking;
using Microsoft.Extensions.Logging;
using NodaTime;
using RedLockNet;

namespace Elsa
{
    public class RedisLockProvider : IDistributedLockProvider
    {
        private const string Prefix = "elsa";
        private readonly ILogger _logger;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly List<IRedLock> _redLockInstance = new();

        public RedisLockProvider(IDistributedLockFactory distributedLockFactory, ILogger<RedisLockProvider> logger)
        {
            _logger = logger;
            _distributedLockFactory = distributedLockFactory;
        }

        public Task<bool> AcquireLockAsync(string name, Duration? timeout = default, CancellationToken cancellationToken = default) => CreateLockAsync(name, timeout, cancellationToken);

        private async Task<bool> CreateLockAsync(string name, Duration? timeout = default, CancellationToken cancellationToken = default)
        {
            var resourceName = $"{Prefix}:{name}";
            _logger.LogInformation("Lock provider will try to acquire lock for {ResourceName}", resourceName);
            
            timeout ??= Duration.Zero;
            
            try
            {
                var redLock = await _distributedLockFactory.CreateLockAsync(
                        resourceName, 
                        timeout.Value.ToTimeSpan(),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromMilliseconds(10),
                        cancellationToken);

                if (!redLock.IsAcquired) 
                    return false;
                
                lock (_redLockInstance) 
                    _redLockInstance.Add(redLock);

                _logger.LogInformation("Lock provider acquired lock for {ResourceName}", resourceName);

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire lock for {ResourceName}", resourceName);
                return false;
            }
        }

        public Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var resourceName = $"{Prefix}:{name}";

            _logger.LogInformation("Lock provider will try to release lock for {resourceName}", resourceName);

            try
            {
                lock (_redLockInstance)
                {
                    foreach (var redLock in _redLockInstance)
                    {
                        if (redLock.Resource == $"{Prefix}:{name}")
                        {
                            redLock.Dispose();
                            _redLockInstance.Remove(redLock);
                            _logger.LogInformation("Lock provider released lock for {resourceName}", resourceName);

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to release lock for {resourceName}. Reason > {ex}", resourceName, ex);
            }

            return Task.CompletedTask;
        }
    }
}