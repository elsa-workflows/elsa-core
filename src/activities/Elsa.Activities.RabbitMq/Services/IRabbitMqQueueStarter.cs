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
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(IServiceProvider serviceProvider, RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
        IAsyncEnumerable<RabbitMqBusConfiguration> GetConfigurationsAsync<T>(Func<Trigger, bool>? predicate, IServiceProvider serviceProvider, CancellationToken cancellationToken) where T : IRabbitMqActivity;
    }
}