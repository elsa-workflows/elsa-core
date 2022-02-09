using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Activities.MqttMessageReceived;
using Elsa.Activities.Mqtt.Options;
using Elsa.Activities.Mqtt.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
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

                var receiverConfigs = (await GetConfigurationsAsync(scope.ServiceProvider, workflowId, cancellationToken).ToListAsync(cancellationToken));

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

        private async IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync(IServiceProvider serviceProvider, string workflowDefinitionId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflowRegistry = serviceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = serviceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflowInstanceStore = serviceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflow = await workflowRegistry.GetWorkflowAsync(workflowDefinitionId, VersionOptions.Latest, cancellationToken);

            var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(serviceProvider, workflow, cancellationToken);

            foreach (var activity in workflowBlueprintWrapper.Filter<MqttMessageReceived>())
            {
                var topic = await activity.EvaluatePropertyValueAsync(x => x.Topic, cancellationToken);
                var host = await activity.EvaluatePropertyValueAsync(x => x.Host, cancellationToken);
                var port = await activity.EvaluatePropertyValueAsync(x => x.Port, cancellationToken);
                var username = await activity.EvaluatePropertyValueAsync(x => x.Username, cancellationToken);
                var password = await activity.EvaluatePropertyValueAsync(x => x.Password, cancellationToken);
                var qos = await activity.EvaluatePropertyValueAsync(x => x.QualityOfService, cancellationToken);

                yield return new MqttClientOptions(topic!, host!, port!, username!, password!, qos);
            }
        }
    }
}