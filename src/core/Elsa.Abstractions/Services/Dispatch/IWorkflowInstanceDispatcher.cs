using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    /// <summary>
    /// Dispatches requests for executing workflow instances.
    /// </summary>
    public interface IWorkflowInstanceDispatcher
    {
        Task DispatchAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
    }
}