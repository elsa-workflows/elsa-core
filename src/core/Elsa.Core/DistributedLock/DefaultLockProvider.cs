using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLocking;
using NodaTime;

namespace Elsa.DistributedLock
{
    /// <summary>
    /// An in-memory, single-node lock provider. Used by default, and suitable only in single-instance hosting environments.
    /// When hosting in a multi-node environment (kubernetes cluster, docker fleet, web farms etc.), use a distributed implementation.
    /// </summary>
    public class DefaultLockProvider : IDistributedLockProvider
    {
        private readonly IClock _clock;
        private readonly HashSet<string> _locks = new();

        public DefaultLockProvider(IClock clock)
        {
            _clock = clock;
        }

        public async Task<bool> AcquireLockAsync(string name, Duration? timeout = default, CancellationToken cancellationToken = default)
        {
            timeout ??= Duration.Zero;
            var start = _clock.GetCurrentInstant();
            
            Monitor.Enter(_locks);
            try
            {
                while (_locks.Contains(name))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                        
                    if(_clock.GetCurrentInstant() - start >= timeout)
                        return false;
                }

                _locks.Add(name);

                return true;
            }
            finally
            {
                Monitor.Exit(_locks);
            }
        }

        public Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default)
        {
            lock (_locks)
                _locks.Remove(name);

            return Task.CompletedTask;
        }
    }
}