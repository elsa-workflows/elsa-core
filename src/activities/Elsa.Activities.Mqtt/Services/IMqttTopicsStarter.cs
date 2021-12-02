using Elsa.Activities.Mqtt.Options;
using Elsa.Services.Models;
using System.Runtime.CompilerServices;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttTopicsStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(MqttClientOptions config, CancellationToken cancellationToken);
        IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync(Func<IWorkflowBlueprint, bool>? predicate, [EnumeratorCancellation] CancellationToken cancellationToken);
    }
}