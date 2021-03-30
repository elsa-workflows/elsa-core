using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Dispatch
{
    /// <summary>
    /// The correlating dispatcher is responsible for finding workflows correlated by the specified correlation ID.
    /// If no correlated workflows are found, a new one is started.
    /// </summary>
    public interface ICorrelatingWorkflowDispatcher
    {
        Task DispatchAsync(ExecuteCorrelatedWorkflowRequest request, CancellationToken cancellationToken = default);
    }
}