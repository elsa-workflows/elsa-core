using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Options;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttTopicsStarter : IMqttTopicsStarter
    {
        protected readonly ICollection<Worker> _workers;
        protected readonly IServiceScopeFactory _scopeFactory;
        protected readonly ILogger<MqttTopicsStarter> _logger;
        private readonly IMessageReceiverClientFactory _receiverFactory;

        public MqttTopicsStarter(
            IMessageReceiverClientFactory receiverFactory,
            IServiceScopeFactory scopeFactory,
            ILogger<MqttTopicsStarter> logger)
        {
            _receiverFactory = receiverFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _workers = new List<Worker>();
        }

        public virtual async Task CreateWorkersAsync(CancellationToken cancellationToken)
        {
            await DisposeExistingWorkersAsync();

            using var scope = _scopeFactory.CreateScope();

            var configs = (await GetConfigurationsAsync(null, scope.ServiceProvider, cancellationToken).ToListAsync(cancellationToken)).Distinct();

            foreach (var config in configs)
            {
                try
                {
                    _workers.Add(await CreateWorkerAsync(scope.ServiceProvider, config, cancellationToken));
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to create a receiver for topic {Topic}", config.Topic);
                }
            }
        }

        public async IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync(Func<IWorkflowBlueprint, bool>? predicate, IServiceProvider serviceProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflowRegistry = serviceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = serviceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflowInstanceStore = serviceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(MqttMessageReceived)
                select workflow;

            var filteredQuery = predicate == null ? query : query.Where(predicate);

            foreach (var workflow in query)
            {
                // If a workflow is not published, only consider it for processing if it has at least one non-ended workflow instance.
                if (!workflow.IsPublished && !await WorkflowHasNonFinishedWorkflowsAsync(workflow, workflowInstanceStore, cancellationToken))
                    continue;

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

        public async Task<Worker> CreateWorkerAsync(IServiceProvider serviceProvider, MqttClientOptions config, CancellationToken cancellationToken)
        {
            var receiver = await _receiverFactory.GetReceiverAsync(config, cancellationToken);
            return ActivatorUtilities.CreateInstance<Worker>(serviceProvider, receiver, (Func<IMqttClientWrapper, Task>)DisposeReceiverAsync);
        }

        protected async Task DisposeExistingWorkersAsync()
        {
            foreach (var worker in _workers.ToList())
            {
                await worker.DisposeAsync();
                _workers.Remove(worker);
            }
        }

        private async Task DisposeReceiverAsync(IMqttClientWrapper messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);

        private static async Task<bool> WorkflowHasNonFinishedWorkflowsAsync(IWorkflowBlueprint workflowBlueprint, IWorkflowInstanceStore workflowInstanceStore, CancellationToken cancellationToken)
        {
            var count = await workflowInstanceStore.CountAsync(new UnfinishedWorkflowSpecification().WithWorkflowDefinition(workflowBlueprint.Id), cancellationToken);
            return count > 0;
        }
    }
}