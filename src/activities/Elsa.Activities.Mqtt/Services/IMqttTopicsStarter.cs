using Elsa.Activities.Mqtt.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttTopicsStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);

        Task<Worker> CreateWorkerAsync(MqttClientOptions config, CancellationToken cancellationToken);

        Task CreateWorkersAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);

        IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync(CancellationToken cancellationToken);
    }
}