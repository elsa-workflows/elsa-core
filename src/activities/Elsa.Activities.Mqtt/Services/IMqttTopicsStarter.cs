using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Options;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttTopicsStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(IServiceProvider serviceProvider, MqttClientOptions config, CancellationToken cancellationToken);
    }
}