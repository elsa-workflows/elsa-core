using Elsa.DistributedLock;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.DistributedLock
{
    /// <summary>
    /// An in-memory, single-node lock provider. Used by default, and suitable only in single-instance hosting environments.
    /// When hosting in a multi-node environment (kubernetes cluster, docker fleet, web farms etc.), use a distributed implementation.
    /// </summary>
    public class DefaultLockProvider : DistributedLockProvider
    {
        private readonly HashSet<string> locks = new HashSet<string>();

        public override Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
        {
            lock (locks)
            {
                if (locks.Contains(name))
                    return Task.FromResult(false);

                locks.Add(name);

                return Task.FromResult(true);
            }
        }

        public override Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default)
        {
            lock (locks)
                locks.Remove(name);

            return Task.CompletedTask;
        }
    }
}