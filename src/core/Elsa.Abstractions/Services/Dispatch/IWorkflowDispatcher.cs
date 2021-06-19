using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Dispatch
{
    /// <summary>
    /// The correlating dispatcher is responsible for finding workflows correlated by the specified correlation ID.
    /// If no correlated workflows are found, a new ones are started and existing ones are resumed.
    /// </summary>
    public interface IWorkflowDispatcher
    {
        Task DispatchAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default);
    }
}