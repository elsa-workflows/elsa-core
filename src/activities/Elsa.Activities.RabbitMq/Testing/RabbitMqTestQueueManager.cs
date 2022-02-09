using Elsa.Activities.RabbitMq.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Testing
{
    public class RabbitMqTestQueueManager : IRabbitMqTestQueueManager
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IDictionary<string, ICollection<Worker>> _workers;
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public RabbitMqTestQueueManager(
            IRabbitMqQueueStarter rabbitMqQueueStarter,
            ILogger<RabbitMqTestQueueManager> logger,
            IServiceScopeFactory scopeFactory)
        {
            _rabbitMqQueueStarter = rabbitMqQueueStarter;
            _logger = logger;
            _workers = new Dictionary<string, ICollection<Worker>>();
            _scopeFactory = scopeFactory;
        }

        public async Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            using var scope = _scopeFactory.CreateScope();

            try
            {

                if (_workers.ContainsKey(workflowInstanceId))
                {
                    if (_workers[workflowInstanceId].Count > 0)
                        return;
                }
                else
                    _workers[workflowInstanceId] = new List<Worker>();

                var workerConfigs = (await _rabbitMqQueueStarter.GetConfigurationsAsync<RabbitMqMessageReceived>(x => x.WorkflowDefinitionId == workflowId, scope.ServiceProvider, cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var config in workerConfigs)
                {
                    try
                    {
                        _workers[workflowInstanceId].Add(await _rabbitMqQueueStarter.CreateWorkerAsync(scope.ServiceProvider, config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a test receiver for routing key {RoutingKey}", config.RoutingKey);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task TryDisposeTestWorkersAsync(string workflowInstance)
        {
            if (!_workers.ContainsKey(workflowInstance)) return;

            foreach (var worker in _workers[workflowInstance])
            {
                await worker.DisposeAsync();
            }

            _workers[workflowInstance].Clear();
        }
    }
}