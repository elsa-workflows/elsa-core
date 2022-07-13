using Elsa.Activities.Mqtt.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Testing
{
    public class MqttTestClientManager : IMqttTestClientManager
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IDictionary<string, ICollection<Worker>> _workers;
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        private readonly ILogger _logger;

        public MqttTestClientManager(
            IMqttTopicsStarter mqttTopicsStarter,
            ILogger<MqttTestClientManager> logger)
        {
            _mqttTopicsStarter = mqttTopicsStarter;
            _logger = logger;
            _workers = new Dictionary<string, ICollection<Worker>>();
        }

        public async Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!_workers.ContainsKey(workflowInstanceId))
                {
                    _workers[workflowInstanceId] = new List<Worker>();
                }

                var receiverConfigs = (await _mqttTopicsStarter.GetConfigurationsAsync(cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var config in receiverConfigs)
                {
                    try
                    {
                        _workers[workflowInstanceId].Add(await _mqttTopicsStarter.CreateWorkerAsync(config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a test receiver for topic {Topic}", config.Topic);
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