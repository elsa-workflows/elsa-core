using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Testing
{
    public interface IMqttTestClientManager
    {
        Task CreateTestWorkersAsync(IServiceProvider serviceProvider, string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default);
        Task TryDisposeTestWorkersAsync(string workflowInstanceId);
    }
}
