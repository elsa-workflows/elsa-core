using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;

namespace Elsa.Activities.RabbitMq.Testing
{
    public interface IRabbitMqTestQueueManager
    {
        Task CreateTestWorkersAsync(ITenant tenant, string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default);
        Task TryDisposeTestWorkersAsync(string workflowInstanceId);
    }
}