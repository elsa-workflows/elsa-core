using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Dispatch
{
    /// <summary>
    /// Dispatches requests for executing workflow definitions.
    /// </summary>
    public interface IWorkflowDefinitionDispatcher
    {
        Task DispatchAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    }
}