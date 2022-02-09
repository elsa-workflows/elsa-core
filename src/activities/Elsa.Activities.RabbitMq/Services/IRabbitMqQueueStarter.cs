using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Configuration;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IRabbitMqQueueStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(IServiceProvider serviceProvider, RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
    }
}