using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Configuration;
using Elsa.Models;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IRabbitMqQueueStarter
    {
        Task CreateWorkersAsync(IReadOnlyCollection<Trigger> triggers, IServiceProvider services, CancellationToken cancellationToken = default);
        Task CreateWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, IServiceProvider services, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(RabbitMqBusConfiguration clientOptions, IServiceProvider services, CancellationToken cancellationToken);
    }
}