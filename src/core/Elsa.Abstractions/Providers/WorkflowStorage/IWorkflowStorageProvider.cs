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
        ValueTask SaveAsync(CancellationToken cancellationToken = default);
        ValueTask<object?> LoadAsync(CancellationToken cancellationToken = default);
    }
}