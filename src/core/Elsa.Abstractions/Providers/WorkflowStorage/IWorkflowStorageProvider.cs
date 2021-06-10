using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Providers.WorkflowStorage
{
    /// <summary>
    /// A strategy for storing workflow data such as variables and output properties.
    /// </summary>
    public interface IWorkflowStorageProvider
    {
        string Name { get; }
        string DisplayName { get; }
        ValueTask SaveAsync(ActivityExecutionContext context, string propertyName, object? value, CancellationToken cancellationToken = default);
        ValueTask<object?> LoadAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default);
    }
}