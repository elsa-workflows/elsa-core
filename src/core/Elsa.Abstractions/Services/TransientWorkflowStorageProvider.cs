using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowStorage;

namespace Elsa.Services
{
    /// <summary>
    /// Stores values in memory.
    /// </summary>
    public class TransientWorkflowStorageProvider : WorkflowStorageProvider
    {
        public override ValueTask SaveAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override ValueTask<object?> LoadAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}