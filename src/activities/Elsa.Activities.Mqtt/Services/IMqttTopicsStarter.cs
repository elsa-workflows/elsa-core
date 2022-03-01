using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Options;
using Elsa.Models;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttTopicsStarter
    {
        Task CreateWorkersAsync(IReadOnlyCollection<Trigger> triggers, IServiceProvider services, CancellationToken cancellationToken = default);
        Task CreateWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, IServiceProvider services, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(MqttClientOptions clientOptions, IServiceProvider services, CancellationToken cancellationToken);
    }
}