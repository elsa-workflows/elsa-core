using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Providers.WorkflowStorage
{
    /// <summary>
    /// A strategy for storing workflow data such as variables and output properties.
    /// </summary>
    public interface IWorkflowStorageProvider
    {
        string Name { get; }
        string DisplayName { get; }
        ValueTask SaveAsync(WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default);
        ValueTask<object?> LoadAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(WorkflowStorageContext context, CancellationToken cancellationToken = default);
    }
}