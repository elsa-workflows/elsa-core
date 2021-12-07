using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Testing
{
    public interface IRabbitMqTestQueueManager
    {
        Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default);
        Task DisposeTestWorkersAsync(string workflowInstanceId);
    }
}