using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.DistributedLock
{
    /// <summary>
    /// An in-memory, single-node lock provider. Used by default, and suitable only in single-instance hosting environments.
    /// When hosting in a multi-node environment (kubernetes cluster, docker fleet, web farms etc.), use a distributed implementation.
    /// </summary>
    public class DefaultLockProvider : IDistributedLockProvider
    {
        private readonly HashSet<string> _locks = new HashSet<string>();

        public Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
        {
            lock (_locks)
            {
                if (_locks.Contains(name))
                    return Task.FromResult(false);

                _locks.Add(name);

                return Task.FromResult(true);
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