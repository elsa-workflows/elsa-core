using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Testing
{
    public interface IOpcUaTestQueueManager
    {
        Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default);
        Task DisposeTestWorkersAsync(string workflowInstanceId);
    }
}