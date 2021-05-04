using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;
using NodaTime;

namespace Elsa.Services
{
    /// <summary>
    /// Provides functionality to acquire and release locks which are distributed across all instances in a web farm.
    /// </summary>
    /// <remarks>
    /// Locks can be used to protect critical sections that should only ever be executed by a single thread of execution across a whole web farm.
    /// </remarks>
    public interface IDistributedLockProvider
    {
        Task<IDistributedSynchronizationHandle?> AcquireLockAsync(string name, Duration? timeout = default, CancellationToken cancellationToken = default);
    }
}