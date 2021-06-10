using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Providers.WorkflowStorage
{
    public abstract class WorkflowStorageProvider : IWorkflowStorageProvider
    {
        public string Name => GetType().Name.Replace("WorkflowStorageProvider", "");
        public string DisplayName => Name;
        public abstract ValueTask SaveAsync(CancellationToken cancellationToken = default);
        public abstract ValueTask<object?> LoadAsync(CancellationToken cancellationToken = default);
    }
}