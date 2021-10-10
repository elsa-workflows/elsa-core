using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;

namespace Elsa.Services.Locking
{
    public class FailsafeHandle : IDistributedSynchronizationHandle
    {
        private readonly IDistributedSynchronizationHandle _handle;

        public FailsafeHandle(IDistributedSynchronizationHandle handle)
        {
            _handle = handle;
        }

        public void Dispose()
        {
            try
            {
                _handle.Dispose();
            }
            catch
            {
                // Fail silently; if e.g. the DB connection failed, lock is lost anyway.
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _handle.DisposeAsync();
            }
            catch
            {
                // Fail silently; if e.g. the DB connection failed, lock is lost anyway.
            }
        }

        public CancellationToken HandleLostToken => _handle.HandleLostToken;
    }
}