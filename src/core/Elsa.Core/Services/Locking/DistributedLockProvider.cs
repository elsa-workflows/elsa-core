using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa.Services.Locking
{
    public class DistributedLockProvider : IDistributedLockProvider
    {
        private readonly Func<string, IDistributedLock> _lockFactory;
        private readonly ILogger<DistributedLockProvider> _logger;

        public DistributedLockProvider(Func<string, IDistributedLock> lockFactory, ILogger<DistributedLockProvider> logger)
        {
            _lockFactory = lockFactory;
            _logger = logger;
        }

        public async Task<IDistributedSynchronizationHandle?> AcquireLockAsync(string name, Duration? timeout = default, CancellationToken cancellationToken = default)
        {
            var distributedLock = _lockFactory(name);
            var timeoutTimeSpan = timeout?.ToTimeSpan() ?? TimeSpan.Zero;

            _logger.LogDebug("Acquiring a lock on {LockName}", name);

            var handle = await distributedLock.TryAcquireAsync(timeoutTimeSpan, cancellationToken);

            if (handle == null!)
                return null;

            _logger.LogDebug("Lock acquired on {LockName}", name);

            return new FailsafeHandle(handle);
        }
    }
}