using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Microsoft.Extensions.Logging;
using RedLockNet;

namespace Elsa
{
    public class RedisLockProvider : IDistributedLockProvider
    {
        private const string Prefix = "elsa";
        private readonly ILogger _logger;
        private readonly TimeSpan _lockTimeout;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly List<IRedLock> _redLockInstance = new List<IRedLock>();

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
            _logger.LogInformation("Lock provider will try to acquire lock for {resourceName}",resourceName);
            try
            {
                var redLock = await _distributedLockFactory.CreateLockAsync(resourceName, _lockTimeout,
                                                                           TimeSpan.FromSeconds(1),
                                                                           TimeSpan.FromMilliseconds(10),
                                                                           cancellationToken)
                                                           .ConfigureAwait(false);

                if (redLock.IsAcquired)
                {
                    
                    lock (_redLockInstance)
                    {
                        _redLockInstance.Add(redLock);
                    }
                    _logger.LogInformation("Lock provider acquired lock for {resourceName}",resourceName);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to acquire lock for {resourceName}. Reason > {ex}",resourceName,ex);
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
                            _logger.LogInformation("Lock provider released lock for {resourceName}",resourceName);

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to release lock for {resourceName}. Reason > {ex}",resourceName,ex);
            }
            return Task.CompletedTask;
        }

    }
}