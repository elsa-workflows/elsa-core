using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Providers.WorkflowStorage
{
    public abstract class WorkflowStorageProvider : IWorkflowStorageProvider
    {
        public virtual string Name => GetType().Name.Replace("WorkflowStorageProvider", "");
        public virtual string DisplayName => Name;
        public abstract ValueTask SaveAsync(WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default);
        public abstract ValueTask<object?> LoadAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
        public abstract ValueTask DeleteAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
        public abstract ValueTask DeleteAsync(WorkflowStorageContext context, CancellationToken cancellationToken = default);
    }
}