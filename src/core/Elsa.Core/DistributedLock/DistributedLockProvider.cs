using System.Threading;
using System.Threading.Tasks;

namespace Elsa.DistributedLock
{
    public abstract class DistributedLockProvider : IDistributedLockProvider
    {
        public abstract Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken = default);

        public abstract Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default);

        public virtual Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> SetupAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}