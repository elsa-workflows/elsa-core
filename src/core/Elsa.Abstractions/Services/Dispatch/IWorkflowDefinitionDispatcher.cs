using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;

namespace Elsa.Services
{
    /// <summary>
    /// Dispatches requests for executing workflow definitions.
    /// </summary>
    public interface IWorkflowDefinitionDispatcher
    {
        Task DispatchAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    }
}