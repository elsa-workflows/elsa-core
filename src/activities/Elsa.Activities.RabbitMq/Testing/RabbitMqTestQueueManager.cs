using Elsa.Activities.RabbitMq.Services;
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
        private readonly IDictionary<string, ICollection<WorkerBase>> _workers;
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        private readonly ILogger _logger;

        public RabbitMqTestQueueManager(
            IRabbitMqQueueStarter rabbitMqQueueStarter,
            ILogger<RabbitMqTestQueueManager> logger)
        {
            _rabbitMqQueueStarter = rabbitMqQueueStarter;
            _logger = logger;
            _workers = new Dictionary<string, ICollection<WorkerBase>>();
        }

        public async Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!_workers.ContainsKey(workflowInstanceId))
                {
                    _workers[workflowInstanceId] = new List<WorkerBase>();
                }                    

                var receiverConfigs = (await _rabbitMqQueueStarter.GetConfigurationsAsync<RabbitMqMessageReceived>(x => x.Id == workflowId, cancellationToken).ToListAsync(cancellationToken)).Distinct();
                var senderConfigs = (await _rabbitMqQueueStarter.GetConfigurationsAsync<SendRabbitMqMessage>(x => x.Id == workflowId, cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var config in receiverConfigs)
                {
                    try
                    {
                        _workers[workflowInstanceId].Add(await _rabbitMqQueueStarter.CreateReceiverWorkerAsync(config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a test receiver for routing key {RoutingKey}", config.RoutingKey);
                    }
                }

                foreach (var config in senderConfigs)
                {
                    try
                    {
                        _workers[workflowInstanceId].Add(await _rabbitMqQueueStarter.CreateSenderWorkerAsync(config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a test sender for topic {RoutingKey}", config.RoutingKey);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisposeTestWorkersAsync(string workflowInstance)
        {
            foreach (var worker in _workers[workflowInstance])
            {
                await worker.DisposeAsync();
            }

            _workers[workflowInstance].Clear();
        }
    }
}