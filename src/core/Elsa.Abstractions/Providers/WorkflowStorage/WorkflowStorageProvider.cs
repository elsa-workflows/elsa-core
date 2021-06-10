using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Providers.WorkflowStorage
{
    public abstract class WorkflowStorageProvider : IWorkflowStorageProvider
    {
        public string Name => GetType().Name.Replace("WorkflowStorageProvider", "");
        public string DisplayName => Name;
        public abstract ValueTask SaveAsync(ActivityExecutionContext context, string propertyName, object? value, CancellationToken cancellationToken = default);
        public abstract ValueTask<object?> LoadAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default);
        public abstract ValueTask DeleteAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default);
        public abstract ValueTask DeleteAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default);
    }
}