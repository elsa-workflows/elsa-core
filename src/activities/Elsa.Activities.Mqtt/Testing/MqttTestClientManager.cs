using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Mqtt.Testing
{
    public class MqttTestClientManager : IMqttTestClientManager
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IDictionary<string, ICollection<Worker>> _workers;
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public MqttTestClientManager(
            IMqttTopicsStarter mqttTopicsStarter,
            ILogger<MqttTestClientManager> logger,
            IServiceScopeFactory scopeFactory)
        {
            _mqttTopicsStarter = mqttTopicsStarter;
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
                if (!_workers.ContainsKey(workflowInstanceId))
                {
                    _workers[workflowInstanceId] = new List<Worker>();
                }

                var receiverConfigs = (await _mqttTopicsStarter.GetConfigurationsAsync(x => x.Id == workflowId, scope.ServiceProvider, cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var config in receiverConfigs)
                {
                    try
                    {
                        _workers[workflowInstanceId].Add(await _mqttTopicsStarter.CreateWorkerAsync(scope.ServiceProvider, config, cancellationToken));
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