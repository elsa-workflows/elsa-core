using Elsa.Activities.Mqtt.Options;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttTopicsStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(MqttClientOptions config, CancellationToken cancellationToken);
        IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync(Func<IWorkflowBlueprint, bool>? predicate, CancellationToken cancellationToken);
    }
}