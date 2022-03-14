using Elsa.Activities.OpcUa.Configuration;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Services
{
    public interface IOpcUaQueueStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<Worker> CreateWorkerAsync(OpcUaBusConfiguration config, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OpcUaBusConfiguration> GetConfigurationsAsync(CancellationToken cancellationToken);
    }
}