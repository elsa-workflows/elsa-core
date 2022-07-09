using Elsa.Activities.RabbitMq.Configuration;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IRabbitMqQueueStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
    }
}